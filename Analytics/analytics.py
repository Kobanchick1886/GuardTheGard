import tkinter as tk
import customtkinter as ctk
import pandas as pd
import matplotlib.pyplot as plt
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg


# ---------------------------------------------------------
# Налаштування зовнішнього вигляду
# ---------------------------------------------------------

ctk.set_appearance_mode("light")
ctk.set_default_color_theme("blue")


# ---------------------------------------------------------
# Основний клас програми
# ---------------------------------------------------------

class AnalyticsApp(ctk.CTk):

    def __init__(self):
        super().__init__()

        self.title("Система аналізу ігрової терапії")
        self.geometry("1100x750")

        # Початкові значення
        self.df = None
        self.patient_df = None
        self.session_df = None
        self.current_patient = None
        self.current_session = None

        # -------------------------------------------------
        # Завантаження даних
        # -------------------------------------------------

        try:
            self.df = pd.read_csv(r"C:\Users\%USERNAME%\AppData\LocalLow\DefaultCompany\Mutanica\DefeatAnalytics.csv")

            self.df["Timestamp"] = pd.to_datetime(
                self.df["Timestamp"],
                dayfirst=True,
                errors="coerce"
            )
            self.df["Success"] = (self.df["MowerKills"].fillna(0)+ self.df["ScissorsKills"].fillna(0))
            self.df["TotalActions"] = (self.df["Success"]+ self.df["TotalMissed"].fillna(0))
            self.df["SuccessPercent"] = (self.df["Success"]/ self.df["TotalActions"]* 100).fillna(0)
            self.df["MissPercent"] = ( self.df["TotalMissed"].fillna(0)/ self.df["TotalActions"]* 100).fillna(0)

            # Список пацієнтів
            self.patients = sorted(
                self.df["PlayerName"]
                .dropna()
                .unique()
                .tolist()
            )

        except Exception as e:
            print(f"Помилка завантаження CSV: {e}")
            self.df = None
            self.patients = []

        # -------------------------------------------------
        # Налаштування сітки
        # -------------------------------------------------

        self.grid_columnconfigure(1, weight=1)
        self.grid_rowconfigure(0, weight=1)

        # -------------------------------------------------
        # Бокове меню
        # -------------------------------------------------

        self.navigation_frame = ctk.CTkFrame(
            self,
            corner_radius=0,
            width=200
        )

        self.navigation_frame.grid(
            row=0,
            column=0,
            sticky="nsew"
        )

        self.navigation_frame.grid_propagate(False)

        self.menu_label = ctk.CTkLabel(
            self.navigation_frame,
            text="МЕНЮ",
            font=ctk.CTkFont(
                size=18,
                weight="bold"
            )
        )

        self.menu_label.pack(
            pady=(30, 25),
            padx=20
        )

        self.btn_patient = ctk.CTkButton(
            self.navigation_frame,
            text="Обрати пацієнта",
            command=self.show_patient_selection
        )

        self.btn_patient.pack(
            pady=10,
            padx=20,
            fill="x"
        )

        self.btn_summary = ctk.CTkButton(
            self.navigation_frame,
            text="Загальна аналітика",
            command=self.open_summary
        )

        self.btn_summary.pack(
            pady=10,
            padx=20,
            fill="x"
        )

        self.btn_session = ctk.CTkButton(
            self.navigation_frame,
            text="Аналіз сесії",
            command=self.choose_session
        )

        self.btn_session.pack(
            pady=10,
            padx=20,
            fill="x"
        )

        self.btn_analysis = ctk.CTkButton(
            self.navigation_frame,
            text="Аналітичний висновок",
            command=self.show_analysis
        )

        self.btn_analysis.pack(
            pady=10,
            padx=20,
            fill="x"
        )

        # -------------------------------------------------
        # Основна область
        # -------------------------------------------------

        self.main_frame = ctk.CTkFrame(
            self,
            corner_radius=0,
            fg_color="transparent"
        )

        self.main_frame.grid(
            row=0,
            column=1,
            sticky="nsew",
            padx=20,
            pady=20
        )

        self.show_patient_selection()

    # =====================================================
    # Допоміжні функції
    # =====================================================

    def clear_frame(self):
        """Очищення основної області."""

        for widget in self.main_frame.winfo_children():
            widget.destroy()

    def get_current_patient(self):
        """Перевіряє, чи обраний пацієнт."""

        if self.current_patient is None:
            self.show_patient_selection()
            return False

        return True

    # =====================================================
    # Вибір пацієнта
    # =====================================================

    def show_patient_selection(self):

        self.clear_frame()

        title = ctk.CTkLabel(
            self.main_frame,
            text="Оберіть пацієнта",
            font=ctk.CTkFont(
                size=24,
                weight="bold"
            )
        )

        title.pack(pady=(30, 20))

        if not self.patients:
            ctk.CTkLabel(
                self.main_frame,
                text="Не знайдено даних про пацієнтів.",
                font=ctk.CTkFont(size=16)
            ).pack(pady=20)

            return

        self.patient_menu = ctk.CTkOptionMenu(
            self.main_frame,
            values=self.patients,
            width=300
        )

        self.patient_menu.pack(pady=20)

        btn = ctk.CTkButton(
            self.main_frame,
            text="Відкрити загальну статистику",
            command=self.open_summary,
            width=300
        )

        btn.pack(pady=10)

        btn_session = ctk.CTkButton(
            self.main_frame,
            text="Аналіз конкретної сесії",
            command=self.choose_session,
            width=300
        )

        btn_session.pack(pady=10)

    # =====================================================
    # Вибір пацієнта
    # =====================================================

    def select_patient(self):

        if not hasattr(self, "patient_menu"):
            return False

        self.current_patient = self.patient_menu.get()

        self.patient_df = self.df[
            self.df["PlayerName"] == self.current_patient
        ].copy()

        self.patient_df = self.patient_df.sort_values(
            by="SessionNumber"
        ).reset_index(drop=True)

        return True

    # =====================================================
    # Загальна статистика
    # =====================================================

    def open_summary(self):

        if self.df is None:
            return

        # Якщо пацієнт ще не вибраний
        if self.current_patient is None:

            if hasattr(self, "patient_menu"):
                self.select_patient()
            else:
                self.show_patient_selection()
                return

        self.show_summary()

    # =====================================================
    # Загальний звіт
    # =====================================================

    def show_summary(self):

        if self.patient_df is None or self.patient_df.empty:
            self.show_patient_selection()
            return

        self.clear_frame()

        # -------------------------------------------------
        # Основні розрахунки
        # -------------------------------------------------

        first = self.patient_df.iloc[0]
        last = self.patient_df.iloc[-1]

        count_sessions = len(self.patient_df)

        average_missed = self.patient_df[
            "TotalMissed"
        ].mean()
        # Найкраща сесія — мінімум пропусків
        best_index = self.patient_df["TotalMissed"].idxmin()
        best_session = self.patient_df.loc[best_index]
        # Найгірша сесія — максимум пропусків
        worst_index = self.patient_df["TotalMissed"].idxmax()
        worst_session = self.patient_df.loc[worst_index]
        best_session_number = int(best_session["SessionNumber"])
        worst_session_number = int(worst_session["SessionNumber"])
        
        average_success = self.patient_df["SuccessPercent"].mean()

        first_missed = first["TotalMissed"]
        last_missed = last["TotalMissed"]

        if first_missed != 0:
            progress_percent = (
                (first_missed - last_missed)
                / first_missed
                * 100
            )
        else:
            progress_percent = 0

        # Неуспішні дії = пропуски
        total_missed = int(last["TotalMissed"])
        # Успішні дії = всі дії без неуспішних
        total_actions = int(
            last["MowerKills"]
            + last["ScissorsKills"]+ last["TotalMissed"])
        total_kills = total_actions - total_missed
        # 100% дій = успішні + неуспішні
        if total_actions > 0:
            success_percent = (total_kills / total_actions) * 100
            miss_percent = (total_missed / total_actions) * 100
        else:
            total_kills = 0
            success_percent = 0
            miss_percent = 0

        # -------------------------------------------------
        # Заголовок
        # -------------------------------------------------

        header = ctk.CTkLabel(
            self.main_frame,
            text=f"Аналітичний звіт: {self.current_patient}",
            font=ctk.CTkFont(
                size=22,
                weight="bold"
            )
        )

        header.pack(pady=(0, 15))

        # -------------------------------------------------
        # Вкладки
        # -------------------------------------------------

        tabs = ctk.CTkTabview(
            self.main_frame
        )

        tabs.pack(
            fill="both",
            expand=True,
            pady=10
        )

        tab_info = tabs.add("Показники")
        tab_graph = tabs.add("Динаміка")
        tab_side = tabs.add("Напрямки")
        tab_color = tabs.add("Кольори")
        tab_text = tabs.add("Висновок")

        # =================================================
        # Вкладка "Показники"
        # =================================================

        stats_frame = ctk.CTkScrollableFrame(
            tab_info
        )

        stats_frame.pack(
            fill="both",
            expand=True,
            padx=10,
            pady=10
        )

        self.create_metric_card(
            stats_frame,
            "Успішні дії",
            f"{total_kills}\n({success_percent:.1f}%)",
            "#2ecc71"
        ).pack(
            fill="x",
            pady=5
        )

        self.create_metric_card(
            stats_frame,
            "Пропуски",
            f"{total_missed}\n({miss_percent:.1f}%)",
            "#e74c3c"
        ).pack(
            fill="x",
            pady=5
        )

        self.create_metric_card(
            stats_frame,
            "Кількість сесій",
            count_sessions,
            "#3498db"
        ).pack(
            fill="x",
            pady=5
        )

        self.create_metric_card(
            stats_frame,
            "Середній % успіху",
            f"{average_success:.1f}%",
            "#2ecc71"
        ).pack(
            fill="x",
            pady=5
        )

        self.create_metric_card(
            stats_frame,
            "Середні пропуски",
            f"{average_missed:.1f}",
            "#f39c12"
        ).pack(
            fill="x",
            pady=5
        )
        self.create_metric_card(
            stats_frame,
            "Найкраща сесія",
            f"№ {best_session_number}\n"
            f"Пропуски: {int(best_session['TotalMissed'])}",
            "#27ae60"
            ).pack(
                fill="x",
                pady=5
                )

        self.create_metric_card(
            stats_frame,
            "Найгірша сесія",
           f"№ {worst_session_number}\n"
            f"Пропуски: {int(worst_session['TotalMissed'])}",
            "#e74c3c"
        ).pack(
            fill="x",
            pady=5
        )

        self.create_metric_card(
            stats_frame,
            "Прогрес",
            f"{progress_percent:.1f}%",
            "#9b59b6"
        ).pack(
            fill="x",
            pady=5
        )

        # =================================================
        # Вкладка "Динаміка"
        # =================================================

        fig1, ax1 = plt.subplots(
            figsize=(7, 4)
        )

        ax1.plot(
            self.patient_df["SessionNumber"],
            self.patient_df["TotalMissed"],
            marker="o"
        )

        ax1.set_title("Динаміка пропусків")
        ax1.set_xlabel("Сесія")
        ax1.set_ylabel("Кількість пропусків")
        ax1.grid(True)

        fig1.tight_layout()

        canvas1 = FigureCanvasTkAgg(
            fig1,
            master=tab_graph
        )

        canvas1.draw()

        canvas1.get_tk_widget().pack(
            fill="both",
            expand=True,
            padx=10,
            pady=10
        )

        # Другий графік
        fig2, ax2 = plt.subplots(
            figsize=(7, 4)
        )

        ax2.plot(
            self.patient_df["SessionNumber"],
            self.patient_df["SuccessPercent"],
            marker="o"
        )

        ax2.set_title("Динаміка успішності")
        ax2.set_xlabel("Сесія")
        ax2.set_ylabel("Успішність, %")
        ax2.grid(True)

        fig2.tight_layout()

        canvas2 = FigureCanvasTkAgg(
            fig2,
            master=tab_graph
        )

        canvas2.draw()

        canvas2.get_tk_widget().pack(
            fill="both",
            expand=True,
            padx=10,
            pady=10
        )

        # =================================================
        # Вкладка "Напрямки"
        # =================================================

        sides = {
            "Верх": self.patient_df["Top"].sum(),
            "Низ": self.patient_df["Bottom"].sum(),
            "Ліво": self.patient_df["Left"].sum(),
            "Право": self.patient_df["Right"].sum()
        }

        fig3, ax3 = plt.subplots(
            figsize=(7, 4)
        )

        ax3.bar(
            list(sides.keys()),
            list(sides.values())
        )

        ax3.set_title("Пропуски за напрямками")
        ax3.set_ylabel("Кількість пропусків")
        ax3.grid(axis="y")

        fig3.tight_layout()

        canvas3 = FigureCanvasTkAgg(
            fig3,
            master=tab_side
        )

        canvas3.draw()

        canvas3.get_tk_widget().pack(
            fill="both",
            expand=True,
            padx=10,
            pady=10
        )

        # =================================================
        # Вкладка "Кольори"
        # =================================================

        colors = {
            "Синій": self.patient_df["BlueMissed"].sum(),
            "Червоний": self.patient_df["RedMissed"].sum(),
            "Жовтий": self.patient_df["YellowMissed"].sum()
        }

        fig4, ax4 = plt.subplots(
            figsize=(7, 4)
        )

        ax4.bar(
            list(colors.keys()),
            list(colors.values())
        )

        ax4.set_title("Пропуски за кольорами")
        ax4.set_ylabel("Кількість пропусків")
        ax4.grid(axis="y")

        fig4.tight_layout()

        canvas4 = FigureCanvasTkAgg(
            fig4,
            master=tab_color
        )

        canvas4.draw()

        canvas4.get_tk_widget().pack(
            fill="both",
            expand=True,
            padx=10,
            pady=10
        )

        # =================================================
        # Вкладка "Висновок"
        # =================================================

        textbox = ctk.CTkTextbox(
            tab_text
        )

        textbox.pack(
            fill="both",
            expand=True,
            padx=10,
            pady=10
        )

        textbox.insert(
            "1.0",
            self.generate_patient_analysis()
        )

        textbox.configure(
            state="disabled"
        )

    # =====================================================
    # Вибір конкретної сесії
    # =====================================================

    def choose_session(self):

        if self.df is None:
            return

        # Якщо пацієнт ще не вибраний
        if self.current_patient is None:

            if hasattr(self, "patient_menu"):
                self.select_patient()
            else:
                self.show_patient_selection()
                return

        if self.patient_df is None or self.patient_df.empty:
            return

        self.clear_frame()

        title = ctk.CTkLabel(
            self.main_frame,
            text=f"Оберіть сесію: {self.current_patient}",
            font=ctk.CTkFont(
                size=22,
                weight="bold"
            )
        )

        title.pack(pady=20)

        sessions = [
            str(int(x))
            for x in self.patient_df[
                "SessionNumber"
            ].dropna().unique()
        ]

        sessions = sorted(
            sessions,
            key=int
        )

        if not sessions:
            ctk.CTkLabel(
                self.main_frame,
                text="Сесій не знайдено."
            ).pack(pady=20)

            return

        self.session_menu = ctk.CTkOptionMenu(
            self.main_frame,
            values=sessions,
            width=300
        )

        self.session_menu.pack(
            pady=20
        )

        ctk.CTkButton(
            self.main_frame,
            text="Відкрити сесію",
            command=self.open_session,
            width=300
        ).pack(
            pady=10
        )

    # =====================================================
    # Відкрити сесію
    # =====================================================

    def open_session(self):

        try:
            number = int(
                self.session_menu.get()
            )

            self.session_df = self.patient_df[
                self.patient_df["SessionNumber"] == number
            ].copy()

            if self.session_df.empty:
                return

            self.current_session = (
                self.session_df.iloc[0]
            )

            self.show_analysis()

        except Exception as e:
            print(f"Помилка відкриття сесії: {e}")

    # =====================================================
    # Аналітичний висновок конкретної сесії
    # =====================================================

    def generate_analysis(self):

        if self.current_session is None:
            return (
                "Спочатку оберіть пацієнта "
                "та конкретну сесію."
            )

        session = self.current_session

        total = int(
            session["TotalMissed"]
        )

        if total == 0:
            return (
                "ПРОПУСКИ\n\n"
                "Під час тестування пропусків не виявлено.\n\n"
                "Результат сесії є позитивним."
            )

        # -------------------------------------------------
        # Напрямки
        # -------------------------------------------------

        sides = {
            "Верх": int(session["Top"]),
            "Низ": int(session["Bottom"]),
            "Ліво": int(session["Left"]),
            "Право": int(session["Right"])
        }

        # -------------------------------------------------
        # Кольори
        # -------------------------------------------------

        colors = {
            "Синій": int(session["BlueMissed"]),
            "Червоний": int(session["RedMissed"]),
            "Жовтий": int(session["YellowMissed"])
        }

        text = "ПРОПУСКИ ЗА СТОРОНАМИ\n\n"

        for name, value in sides.items():

            percent = (
                value / total * 100
            )

            text += (
                f"• {name}: {value} із "
                f"{total} ({percent:.1f}%)\n"
            )

        text += "\nПРОПУСКИ ЗА КОЛЬОРАМИ\n\n"

        for name, value in colors.items():

            percent = (
                value / total * 100
            )

            text += (
                f"• {name}: {value} із "
                f"{total} ({percent:.1f}%)\n"
            )

        # -------------------------------------------------
        # Найпроблемніший напрямок
        # -------------------------------------------------

        side_name = max(
            sides,
            key=sides.get
        )

        side_value = sides[
            side_name
        ]

        side_percent = (
            side_value / total * 100
        )

        # -------------------------------------------------
        # Найпроблемніший колір
        # -------------------------------------------------

        color_name = max(
            colors,
            key=colors.get
        )

        color_value = colors[
            color_name
        ]

        color_percent = (
            color_value / total * 100
        )

        # -------------------------------------------------
        # Висновок
        # -------------------------------------------------

        text += "\nАНАЛІТИЧНИЙ ВИСНОВОК\n\n"

        if side_percent >= 40:

            text += (
                f"Найбільша кількість пропусків "
                f"припадає на напрямок "
                f"{side_name.lower()} "
                f"({side_value} із {total}, "
                f"{side_percent:.1f}%). "
                "Це може свідчити про зниження "
                "концентрації уваги у цьому напрямку.\n\n"
            )

        else:

            text += (
                f"Пропуски за напрямками "
                f"розподілені відносно рівномірно. "
                f"Найбільше пропусків у напрямку "
                f"{side_name.lower()} "
                f"({side_percent:.1f}%).\n\n"
            )

        if color_percent >= 40:

            text += (
                f"Найбільша кількість пропусків "
                f"припадає на об'єкти "
                f"{color_name.lower()} кольору "
                f"({color_value} із {total}, "
                f"{color_percent:.1f}%). "
                "Це може свідчити про складність "
                "своєчасного реагування "
                "на об'єкти цього кольору.\n\n"
            )

        else:

            text += (
                f"Вираженої проблеми з окремим "
                f"кольором не виявлено. "
                f"Найчастіше пропускалися об'єкти "
                f"{color_name.lower()} кольору "
                f"({color_percent:.1f}%).\n\n"
            )

        if (
            side_percent >= 40
            and color_percent >= 40
        ):

            text += (
                "Одночасно спостерігається "
                "значна концентрація пропусків "
                "за просторовим розташуванням "
                "та кольором. Доцільно провести "
                "додаткове тестування для "
                "уточнення причини."
            )

        return text

    # =====================================================
    # Вікно аналітичного висновку
    # =====================================================

    def show_analysis(self):

        if self.current_session is None:

            self.clear_frame()

            header = ctk.CTkLabel(
                self.main_frame,
                text="Аналітичний висновок",
                font=ctk.CTkFont(
                    size=22,
                    weight="bold"
                )
            )

            header.pack(
                pady=(20, 10)
            )

            textbox = ctk.CTkTextbox(
                self.main_frame
            )

            textbox.pack(
                fill="both",
                expand=True,
                padx=10,
                pady=10
            )

            textbox.insert(
                "1.0",
                "Спочатку оберіть пацієнта "
                "та конкретну сесію."
            )

            textbox.configure(
                state="disabled"
            )

            return

        self.clear_frame()

        header = ctk.CTkLabel(
            self.main_frame,
            text=(
                f"Аналіз сесії №"
                f"{int(self.current_session['SessionNumber'])}"
            ),
            font=ctk.CTkFont(
                size=22,
                weight="bold"
            )
        )

        header.pack(
            pady=(0, 15)
        )

        textbox = ctk.CTkTextbox(
            self.main_frame
        )

        textbox.pack(
            fill="both",
            expand=True,
            padx=10,
            pady=10
        )

        textbox.insert(
            "1.0",
            self.generate_analysis()
        )

        textbox.configure(
            state="disabled"
        )

    # =====================================================
    # Аналіз пацієнта за всіма сесіями
    # =====================================================

    def generate_patient_analysis(self):

        if self.patient_df is None:
            return "Дані пацієнта відсутні."

        first = self.patient_df.iloc[0]
        last = self.patient_df.iloc[-1]

        text = ""

        text += (
            f"Кількість сесій: "
            f"{len(self.patient_df)}\n\n"
        )

        # -------------------------------------------------
        # Динаміка
        # -------------------------------------------------

        if (
            last["TotalMissed"]
            < first["TotalMissed"]
        ):

            text += (
                "Спостерігається позитивна "
                "динаміка. Кількість пропусків "
                "зменшується.\n\n"
            )

        elif (
            last["TotalMissed"]
            > first["TotalMissed"]
        ):

            text += (
                "Спостерігається погіршення "
                "результатів. Кількість пропусків "
                "збільшилася.\n\n"
            )

        else:

            text += (
                "Результати залишаються "
                "стабільними.\n\n"
            )

        # -------------------------------------------------
        # Аналіз напрямків
        # -------------------------------------------------

        sides = {
            "Верх": self.patient_df["Top"].sum(),
            "Низ": self.patient_df["Bottom"].sum(),
            "Ліво": self.patient_df["Left"].sum(),
            "Право": self.patient_df["Right"].sum()
        }

        worst_side = max(
            sides,
            key=sides.get
        )

        text += (
            f"Найбільше пропусків "
            f"за напрямком: "
            f"{worst_side}.\n"
        )

        # -------------------------------------------------
        # Аналіз кольорів
        # -------------------------------------------------

        colors = {
            "синього": self.patient_df[
                "BlueMissed"
            ].sum(),

            "червоного": self.patient_df[
                "RedMissed"
            ].sum(),

            "жовтого": self.patient_df[
                "YellowMissed"
            ].sum()
        }

        worst_color = max(
            colors,
            key=colors.get
        )

        text += (
            f"Найчастіше пропускаються "
            f"об'єкти кольору: "
            f"{worst_color}.\n\n"
        )

        # -------------------------------------------------
        # Додатковий висновок
        # -------------------------------------------------

        first_missed = first["TotalMissed"]
        last_missed = last["TotalMissed"]

        if first_missed > 0:

            progress = (
                (first_missed - last_missed)
                / first_missed
                * 100
            )

            if progress > 0:

                text += (
                    f"Загальний прогрес між "
                    f"першою та останньою сесіями "
                    f"становить {progress:.1f}%.\n"
                )

            elif progress < 0:

                text += (
                    f"Результат погіршився на "
                    f"{abs(progress):.1f}% "
                    f"порівняно з першою сесією.\n"
                )

            else:

                text += (
                    "Кількість пропусків між "
                    "першою та останньою сесіями "
                    "не змінилася.\n"
                )

        return text

    # =====================================================
    # Картка показника
    # =====================================================

    def create_metric_card(
        self,
        master,
        title,
        value,
        color
    ):

        card = ctk.CTkFrame(
            master,
            border_width=1,
            border_color="#dddddd"
        )

        ctk.CTkLabel(
            card,
            text=title,
            font=ctk.CTkFont(
                size=14
            )
        ).pack(
            pady=(10, 2)
        )

        ctk.CTkLabel(
            card,
            text=str(value),
            font=ctk.CTkFont(
                size=24,
                weight="bold"
            ),
            text_color=color
        ).pack(
            pady=(0, 10)
        )

        return card


# ---------------------------------------------------------
# Запуск програми
# ---------------------------------------------------------

if __name__ == "__main__":
    app = AnalyticsApp()
    app.mainloop()

