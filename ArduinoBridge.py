import platform, os, time, threading, queue, tempfile, ctypes, importlib, urllib.request, socket
import tkinter as tk
from tkinter import ttk, messagebox
import serial, serial.tools.list_ports

# ---------- optional backends ----------
VIGEM_URL = "https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe"
VIGEM_INSTALLER = os.path.join(tempfile.gettempdir(), "ViGEmBus_1.22.0.exe")

def install_vigem():
    try:
        urllib.request.urlretrieve(VIGEM_URL, VIGEM_INSTALLER)
        return ctypes.windll.shell32.ShellExecuteW(
            None, "runas", VIGEM_INSTALLER, None, None, 1) > 32
    except Exception:
        return False

def load_vgamepad():
    try:
        return importlib.import_module("vgamepad")
    except Exception as e:
        if "VIGEM_ERROR_BUS_NOT_FOUND" not in str(e) or not install_vigem():
            return None
        time.sleep(5)
        try: return importlib.import_module("vgamepad")
        except Exception: return None

IS_WINDOWS = platform.system() == "Windows"
vg = load_vgamepad() if IS_WINDOWS else None

try:
    from pynput import mouse
    HAS_PYNPUT = True
except ImportError:
    HAS_PYNPUT = False

try:
    import uinput
    HAS_UINPUT = True
except ImportError:
    HAS_UINPUT = False

HAS_GAMEPAD = vg is not None or HAS_UINPUT

# ---------- constants ----------
CENTER, CTRL_OFFSET = 500, 100
POLL_INTERVAL, RETRY = .02, 2
DEAD_MAX, DEAD_DISPLAY = 20, 50
DOUBLE_CLICK, RIGHT_HOLD, RIGHT_DOUBLE = .3, .2, .2
UDP_PORT = 5005

CHIPS = {
    (0x1A86,0x7523):"CH340", (0x1A86,0x5523):"CH341",
    (0x1A86,0x5512):"CH341", (0x0403,0x6001):"FTDI",
    (0x0403,0x6015):"FTDI", (0x10C4,0xEA60):"CP2102",
    (0x2341,0x0043):"Arduino Uno", (0x2341,0x0001):"Arduino Uno",
    (0x2A03,0x0043):"Arduino.org Uno", (0x1B4F,0x9206):"Pro Micro"
}
HINTS = ("CH340","CH341","FTDI","FT232","CP210","USB2.0-SERIAL",
         "USB-SERIAL","SILICON LABS","USB SERIAL")

def find_port():
    ports_list = list(serial.tools.list_ports.comports())
    for p in ports_list:
        if (p.vid, p.pid) in CHIPS: return p.device
    for p in ports_list:
        s = " ".join(filter(None, (p.description, p.manufacturer, p.hwid))).upper()
        if any(x in s for x in HINTS): return p.device
    usb = [p.device for p in ports_list if "USB" in p.device.upper() or "ACM" in p.device.upper()]
    return usb[0] if len(usb) == 1 else None

def ports(): return [p.device for p in serial.tools.list_ports.comports()]
def packet(h,l): return (h >> 3) & 15, ((h & 7) << 7) | (l & 127)
def clamp(v,a,b): return max(a,min(b,v))

# ---------- WinAPI 32/64-bit compliant SendInput & keybd_event ----------
if IS_WINDOWS:
    ULONG_PTR = ctypes.c_ulonglong if ctypes.sizeof(ctypes.c_void_p) == 8 else ctypes.c_ulong

    class _MOUSEINPUT(ctypes.Structure):
        _fields_ = [("dx", ctypes.c_long), ("dy", ctypes.c_long),
                    ("mouseData", ctypes.c_ulong), ("dwFlags", ctypes.c_ulong),
                    ("time", ctypes.c_ulong), ("dwExtraInfo", ULONG_PTR)]

    class _KEYBDINPUT(ctypes.Structure):
        _fields_ = [("wVk", ctypes.c_ushort), ("wScan", ctypes.c_ushort),
                    ("dwFlags", ctypes.c_ulong), ("time", ctypes.c_ulong),
                    ("dwExtraInfo", ULONG_PTR)]

    class _HARDWAREINPUT(ctypes.Structure):
        _fields_ = [("uMsg", ctypes.c_ulong), ("wParamL", ctypes.c_short), ("wParamH", ctypes.c_ushort)]

    class _INPUT(ctypes.Structure):
        class _U(ctypes.Union):
            _fields_ = [("mi", _MOUSEINPUT), ("ki", _KEYBDINPUT), ("hi", _HARDWAREINPUT)]
        _anonymous_ = ("_u",)
        _fields_ = [("type", ctypes.c_ulong), ("_u", _U)]

    _KEYEVENTF_KEYUP    = 0x0002
    _KEYEVENTF_SCANCODE = 0x0008

    # Scan codes (Set 1)
    _SC = {
        "w": 0x11, "a": 0x1E, "s": 0x1F, "d": 0x20,
        "e": 0x12, "space": 0x39, "shift": 0x2A,
        "ctrl": 0x1D, "f5": 0x3F,
    }

    # Virtual key codes
    _VK = {
        "w": 0x57, "a": 0x41, "s": 0x53, "d": 0x44,
        "e": 0x45, "space": 0x20, "shift": 0x10,
        "ctrl": 0x11, "f5": 0x74,
    }

    def _send_key(k, up=False):
        sc = _SC.get(k, 0)
        vk = _VK.get(k, 0)
        flags = _KEYEVENTF_SCANCODE | (_KEYEVENTF_KEYUP if up else 0)

        # 1. Primary: SendInput with exact 64-bit struct, both wVk and wScan
        inp = _INPUT(type=1, ki=_KEYBDINPUT(
            wVk=vk, wScan=sc, dwFlags=flags, time=0, dwExtraInfo=0))
        ctypes.windll.user32.SendInput(1, ctypes.byref(inp), ctypes.sizeof(inp))

        # 2. Universal fallback: keybd_event (bypasses UIPI / struct size differences)
        keybd_flags = (_KEYEVENTF_KEYUP if up else 0)
        ctypes.windll.user32.keybd_event(vk, sc, keybd_flags, 0)

# ---------- outputs ----------
class KeyboardOutput:
    def __init__(self, output_q):
        if not IS_WINDOWS:
            raise RuntimeError("Клавиатурный режим поддерживается только на Windows")
        self.q = output_q
        self.down  = {k: False for k in ("w", "a", "s", "d", "ctrl")}
        self.extra = {"shift": False, "space": False}

    def show(self):
        keys  = [k.upper() for k, v in self.down.items()  if v]
        keys += [k.upper() for k, v in self.extra.items() if v]
        self.q.put(("keys", " + ".join(keys) if keys else "—"))

    def press(self, k):
        if not self.down[k]:
            _send_key(k); self.down[k] = True; self.show()

    def release(self, k):
        if self.down[k]:
            _send_key(k, up=True); self.down[k] = False; self.show()

    def extra_press(self, k):
        if not self.extra[k]:
            _send_key(k); self.extra[k] = True; self.show()

    def extra_release(self, k):
        if self.extra[k]:
            _send_key(k, up=True); self.extra[k] = False; self.show()

    def update_axes(self, x, y, dead):
        th = max(dead, 15)
        (self.press("s"),   self.release("w")) if x > CENTER+th else \
        (self.press("w"),   self.release("s")) if x < CENTER-th else \
        (self.release("w"), self.release("s"))
        (self.press("d"),   self.release("a")) if y > CENTER+th else \
        (self.press("a"),   self.release("d")) if y < CENTER-th else \
        (self.release("a"), self.release("d"))
        self.press("ctrl") if x < CENTER-CTRL_OFFSET else self.release("ctrl")

    def jump(self):          self.extra_press("space")
    def stop_jump(self):     self.extra_release("space")
    def middle_down(self):   self.extra_press("shift")
    def middle_up(self):     self.extra_release("shift")
    def middle_double(self): _send_key("f5"); _send_key("f5", up=True)
    def right_double(self):  _send_key("e");  _send_key("e",  up=True)

    def release_all(self):
        for k in list(self.down.keys()): self.release(k)
        self.extra_release("shift")
        self.extra_release("space")

class GamepadOutput:
    def __init__(self, output_q):
        self.q = output_q
        if vg is not None:
            self.kind, self.pad = "vgamepad", vg.VX360Gamepad()
        elif HAS_UINPUT:
            self.kind = "uinput"
            try:
                self.pad = uinput.Device(
                    (uinput.ABS_X+(-32768,32767,0,0),
                     uinput.ABS_Y+(-32768,32767,0,0)),
                    name="Arduino Virtual Gamepad")
            except Exception as e:
                raise RuntimeError(f"Не удалось открыть /dev/uinput: {e}")
        else:
            raise RuntimeError("Нет backend геймпада: vgamepad или python-uinput")

    def stick(self,x,y):
        if self.kind == "vgamepad":
            self.pad.left_joystick(x_value=int(x),y_value=int(y)); self.pad.update()
        else:
            self.pad.emit(uinput.ABS_X,int(x),syn=False)
            self.pad.emit(uinput.ABS_Y,int(y),syn=True)
        self.q.put(("keys",f"LX: {x:+6d}   LY: {y:+6d}"))

    def update_axes(self,ax,ay,dead):
        x,y=ay-CENTER,CENTER-ax
        x=0 if abs(x)<dead else x; y=0 if abs(y)<dead else y
        m=50 # Reach 100% full stick at delta 50 (comfortable tilt)
        self.stick(clamp(x/m*32767,-32768,32767),
                   clamp(y/m*32767,-32768,32767))

    def release_all(self): self.stick(0,0)
    jump=stop_jump=middle_down=middle_up=middle_double=right_double=lambda self:None

# ---------- bridge ----------
class Bridge:
    def __init__(self,status_q,data_q,output_q):
        self.status_q,self.data_q,self.output_q=status_q,data_q,output_q
        self.ser=None; self.running=False; self.output=None
        self.mode="keyboard"; self.dead=0; self.autoconnect=True; self.manual=None
        self.right=False; self.right_t=0; self.mid_t=0; self._mouse=None
        self.udp_sock=None

    def status(self,s,kind="status"): self.status_q.put((kind,s))
    def set_dead(self,v): self.dead=v

    def click(self,x,y,b,pressed):
        if not self.output or not HAS_PYNPUT: return
        now=time.time()
        if b==mouse.Button.middle:
            if pressed:
                if now-self.mid_t<DOUBLE_CLICK: self.output.middle_double()
                self.mid_t=now; self.output.middle_down()
            else: self.output.middle_up()
        elif b==mouse.Button.right:
            if pressed:
                if now-self.right_t<RIGHT_DOUBLE: self.output.right_double()
                self.right=True; self.right_t=now
            else: self.right=False; self.output.stop_jump()

    def start(self,mode="keyboard",auto=True,manual=None):
        if self.running:return
        self.mode,self.autoconnect,self.manual=mode,auto,manual
        try:
            self.output=KeyboardOutput(self.output_q) if mode=="keyboard" else GamepadOutput(self.output_q)
        except RuntimeError as e: self.status(str(e),"error"); return

        try:
            self.udp_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        except Exception:
            self.udp_sock = None

        if HAS_PYNPUT and not self._mouse:
            try:
                self._mouse=mouse.Listener(on_click=self.click); self._mouse.start()
            except Exception:
                self._mouse=None

        self.running=True
        threading.Thread(target=self.run,daemon=True).start()

    def stop(self):
        self.running=False
        if self.ser:
            try:self.ser.close()
            except:pass
            self.ser=None
        if self.output:self.output.release_all(); self.output=None
        if self.udp_sock:
            try:self.udp_sock.close()
            except:pass
            self.udp_sock=None
        self.status("Остановлено")

    def connect(self):
        while self.running and not self.ser:
            p=find_port() if self.autoconnect else self.manual
            if not p:
                self.status("Плата не найдена — поиск...")
                time.sleep(RETRY); continue
            try:
                self.ser=serial.Serial(p,38400,timeout=0)
                self.status(f"Подключено: {p}","connected")
            except Exception:
                self.status(f"Ошибка подключения: {p}"); time.sleep(RETRY)

    def run(self):
        buf=[]; data={}
        self.connect()
        while self.running:
            t=time.perf_counter()
            try:
                if not self.ser:self.connect(); continue
                while self.ser.in_waiting:
                    b=self.ser.read(1)[0]
                    if not buf:
                        if b>=128:buf=[b]
                    elif b<128:
                        buf.append(b); ch,v=packet(*buf); data[ch]=v; buf=[]
                    else:buf=[]
                if 0 in data and 1 in data and self.output:
                    ax,ay=data[0],data[1]
                    self.output.update_axes(ax,ay,self.dead)

                    # Send UDP tilt packet directly to game
                    if self.udp_sock:
                        try:
                            self.udp_sock.sendto(f"{ax},{ay}".encode(), ('127.0.0.1', UDP_PORT))
                        except Exception:
                            pass

                    if self.right and time.time()-self.right_t>=RIGHT_HOLD:
                        self.output.jump()
                    try:self.data_q.put_nowait((ax,ay))
                    except queue.Full:pass
            except (serial.SerialException,OSError):
                self.status("Плата отключена")
                try:self.ser.close()
                except:pass
                self.ser=None; buf=[]; data={}
                if self.output:self.output.release_all()
            if (d:=POLL_INTERVAL-(time.perf_counter()-t))>0:time.sleep(d)

# ---------- compact GUI ----------
class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("Arduino Controller Bridge"); self.geometry("400x430"); self.resizable(False,False)
        self.sq,self.dq,self.oq=queue.Queue(),queue.Queue(maxsize=1),queue.Queue(maxsize=1)
        self.bridge=Bridge(self.sq,self.dq,self.oq)
        self.mode=tk.StringVar(value="keyboard")
        self.dead=tk.IntVar(value=0); self.auto=tk.BooleanVar(value=True); self.port=tk.StringVar()
        self.build(); self.poll(); self.after(300,self.auto_start)
        self.protocol("WM_DELETE_WINDOW",self.close)

    def build(self):
        p={"padx":10,"pady":5}
        f=ttk.LabelFrame(self,text="Режим"); f.pack(fill="x",**p)
        ttk.Radiobutton(f,text="Keyboard (WASD)",variable=self.mode,value="keyboard").pack(anchor="w",padx=8)
        ttk.Radiobutton(f,text="Joystick (virtual gamepad)",variable=self.mode,value="joystick",
                        state="normal" if HAS_GAMEPAD else "disabled").pack(anchor="w",padx=8)

        f=ttk.LabelFrame(self,text="Dead zone"); f.pack(fill="x",**p)
        self.dlabel=ttk.Label(f,text="0%"); self.dlabel.pack(anchor="e",padx=8)
        ttk.Scale(f,from_=0,to=DEAD_MAX,variable=self.dead,command=self.dead_change).pack(fill="x",padx=8)

        f=ttk.LabelFrame(self,text="Подключение"); f.pack(fill="x",**p)
        ttk.Checkbutton(f,text="Автоподключение",variable=self.auto,
                        command=self.auto_change).pack(anchor="w",padx=8)
        row=ttk.Frame(f); row.pack(fill="x",padx=8)
        self.combo=ttk.Combobox(row,textvariable=self.port,values=ports(),state="disabled")
        self.combo.pack(side="left",fill="x",expand=True)
        ttk.Button(row,text="Обновить",command=lambda:self.combo.configure(values=ports())).pack(side="left",padx=4)

        row=ttk.Frame(self); row.pack(fill="x",**p)
        self.start=ttk.Button(row,text="Старт",command=self.start_bridge); self.start.pack(side="left",expand=True,fill="x",padx=3)
        self.stop=ttk.Button(row,text="Стоп",command=self.stop_bridge,state="disabled"); self.stop.pack(side="left",expand=True,fill="x",padx=3)

        self.status=ttk.Label(self,text="Автозапуск...",foreground="orange"); self.status.pack(fill="x",padx=10,pady=3)
        f=ttk.LabelFrame(self,text="Live Output"); f.pack(fill="both",expand=True,**p)
        self.xy=ttk.Label(f,text="AX: —     AY: —",font=("Consolas",11)); self.xy.pack(pady=5)
        self.out=ttk.Label(f,text="Нажато: —",font=("Consolas",13)); self.out.pack(pady=8)

    def dead_change(self,v):
        v=int(float(v)); self.dead.set(v); self.dlabel.config(text=f"{v/DEAD_MAX*DEAD_DISPLAY:.0f}%"); self.bridge.set_dead(v)

    def auto_change(self):
        self.combo.config(state="disabled" if self.auto.get() else "readonly")

    def auto_start(self): self.start_bridge()

    def start_bridge(self):
        if self.mode.get()=="joystick" and not HAS_GAMEPAD:
            messagebox.showerror("Ошибка","Нет backend виртуального геймпада"); return
        if not self.auto.get() and not self.port.get():
            messagebox.showerror("Ошибка","Выберите COM-порт"); return
        self.bridge.start(self.mode.get(),self.auto.get(),self.port.get() or None)
        self.start.config(state="disabled"); self.stop.config(state="normal")
        self.status.config(text="Запускается...",foreground="orange")

    def stop_bridge(self):
        self.bridge.stop(); self.start.config(state="normal"); self.stop.config(state="disabled")

    def poll(self):
        try:
            while True:
                k,m=self.sq.get_nowait()
                self.status.config(text=m,foreground="red" if k=="error" else "green" if k=="connected" else "orange")
        except queue.Empty:pass
        try:self.xy.config(text="AX: %4d     AY: %4d"%self.dq.get_nowait())
        except queue.Empty:pass
        try:self.out.config(text="Нажато: "+self.oq.get_nowait()[1])
        except queue.Empty:pass
        self.after(50,self.poll)

    def close(self): self.bridge.stop(); self.destroy()

if __name__=="__main__":
    App().mainloop()
