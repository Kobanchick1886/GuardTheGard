using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <summary>
/// Receives Arduino gyroscope tilt data via UDP (port 5005) from the Python bridge
/// and injects it directly into the Movement system, bypassing Windows SendInput
/// which is unreliable with Unity's Input System auto-switching control schemes.
///
/// Attach this component to the same GameObject as Movement and PlayerInput.
/// It works in parallel with normal keyboard/gamepad input — whichever source
/// provides non-zero movement wins.
/// </summary>
public class ArduinoSensorInput : MonoBehaviour
{
    [Header("UDP Settings")]
    [Tooltip("Port to listen on (must match bridge's UDP send port)")]
    [SerializeField] private int listenPort = 5005;

    public void SetListenPort(int port)
    {
        if (listenPort != port)
        {
            listenPort = port;
            if (_running)
            {
                StopUdpListener();
                StartUdpListener();
            }
        }
    }

    [Header("Sensor Calibration")]
    [Tooltip("Center value of the sensor (raw). Typically 500.")]
    [SerializeField] private int sensorCenter = 500;

    [Tooltip("Dead zone in raw sensor units. Values within center ± deadZone are treated as zero.")]
    [SerializeField] private int deadZone = 10;

    [Tooltip("Tilt delta from center to reach 100% full speed. Lower = more sensitive. Default is 50.")]
    [SerializeField] private float maxTiltDelta = 50f;

    [Tooltip("Sensitivity multiplier for sensor tilt.")]
    [SerializeField] private float sensitivityMultiplier = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;

    // --- internal state ---
    private UdpClient _udp;
    private Thread _receiveThread;
    private volatile bool _running;

    // Latest sensor values (written by UDP thread, read by main thread)
    private volatile int _rawAx;  // tilt axis X (forward/back)
    private volatile int _rawAy;  // tilt axis Y (left/right)
    private volatile bool _hasData;
    private long _lastReceiveTicks; // accessed via Interlocked

    // Cached references
    private Movement _movement;
    private PlayerInput _playerInput;

    // The vector we inject
    private Vector2 _sensorMoveInput;

    // Timeout: if no UDP packet for this many seconds, assume sensor disconnected
    private const float SENSOR_TIMEOUT = 0.5f;

    private void Awake()
    {
        _movement = GetComponent<Movement>();
        _playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        StartUdpListener();
    }

    private void OnDisable()
    {
        StopUdpListener();
    }

    private void OnDestroy()
    {
        StopUdpListener();
    }

    private void Update()
    {
        if (!_hasData) return;

        // Check for timeout
        float elapsed = (float)(DateTime.UtcNow.Ticks - System.Threading.Interlocked.Read(ref _lastReceiveTicks)) / TimeSpan.TicksPerSecond;
        if (elapsed > SENSOR_TIMEOUT)
        {
            _sensorMoveInput = Vector2.zero;
            InjectMovement(_sensorMoveInput);
            return;
        }

        // Read raw values
        int ax = _rawAx;
        int ay = _rawAy;

        float moveY = 0f; // up/down in game (W/S -> Y axis)
        float moveX = 0f; // left/right in game (A/D -> X axis)

        int dx = ax - sensorCenter;
        int dy = ay - sensorCenter;

        float effectiveMaxDelta = Mathf.Max(maxTiltDelta, deadZone + 1f);

        // Apply dead zone and responsive tilt curve:
        // ax < center => tilt forward => W => positive Y
        if (Mathf.Abs(dx) > deadZone)
        {
            float sign = dx < 0 ? 1f : -1f;
            float norm = (Mathf.Abs(dx) - deadZone) / (effectiveMaxDelta - deadZone);
            moveY = sign * Mathf.Clamp01(norm * sensitivityMultiplier);
        }

        // ay > center => tilt right => D => positive X
        if (Mathf.Abs(dy) > deadZone)
        {
            float sign = dy > 0 ? 1f : -1f;
            float norm = (Mathf.Abs(dy) - deadZone) / (effectiveMaxDelta - deadZone);
            moveX = sign * Mathf.Clamp01(norm * sensitivityMultiplier);
        }

        _sensorMoveInput = new Vector2(moveX, moveY);

        if (showDebugLog && _sensorMoveInput.sqrMagnitude > 0.01f)
        {
            Debug.Log($"[ArduinoSensor] AX={ax} AY={ay} => Move({moveX:F2}, {moveY:F2})");
        }

        // Only inject sensor input if no keyboard/gamepad input is active
        InjectMovement(_sensorMoveInput);
    }

    /// <summary>
    /// Directly calls Movement.OnMove with a synthetic CallbackContext-like approach.
    /// Since OnMove expects InputAction.CallbackContext which we can't easily construct,
    /// we use reflection-free direct field setting via a public method we add to Movement.
    /// </summary>
    private void InjectMovement(Vector2 move)
    {
        if (_movement != null)
        {
            _movement.SetSensorInput(move);
        }
    }

    // ===================== UDP Networking =====================

    private void StartUdpListener()
    {
        if (_running) return;
        _running = true;

        try
        {
            _udp = new UdpClient();
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.Client.Bind(new IPEndPoint(IPAddress.Loopback, listenPort));
            _udp.Client.ReceiveTimeout = 250; // ms, responsive shutdown
        }
        catch (SocketException ex)
        {
            Debug.LogWarning($"[ArduinoSensor] Could not bind UDP port {listenPort} on 127.0.0.1: {ex.Message}. " +
                             "Sensor input disabled.");
            _running = false;
            if (_udp != null) { try { _udp.Close(); } catch { } _udp = null; }
            return;
        }

        _receiveThread = new Thread(ReceiveLoop)
        {
            IsBackground = true,
            Name = "ArduinoSensorUDP_" + listenPort
        };
        _receiveThread.Start();

        Debug.Log($"[ArduinoSensor] Listening for sensor data on 127.0.0.1:{listenPort}");
    }

    private void StopUdpListener()
    {
        _running = false;

        if (_udp != null)
        {
            try { _udp.Close(); } catch { }
            _udp = null;
        }

        if (_receiveThread != null && _receiveThread.IsAlive)
        {
            _receiveThread.Join(150);
            _receiveThread = null;
        }
    }

    private void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, 0);

        while (_running)
        {
            try
            {
                if (_udp == null) break;
                byte[] data = _udp.Receive(ref remoteEP);
                if (data == null || data.Length == 0) continue;
                string msg = Encoding.UTF8.GetString(data);

                // Expected format: "ax,ay" e.g. "512,498"
                string[] parts = msg.Split(',');
                if (parts.Length >= 2 &&
                    int.TryParse(parts[0], out int ax) &&
                    int.TryParse(parts[1], out int ay))
                {
                    _rawAx = ax;
                    _rawAy = ay;
                    _hasData = true;
                    System.Threading.Interlocked.Exchange(ref _lastReceiveTicks, DateTime.UtcNow.Ticks);
                }
            }
            catch (SocketException)
            {
                // Timeout or close — normal
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch
            {
                // Never throw or call Unity API from background thread
            }
        }
    }
}
