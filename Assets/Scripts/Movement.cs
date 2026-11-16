using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 120f;
    private float drag = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;       // from keyboard/gamepad via InputSystem
    private Vector2 sensorInput;     // from Arduino sensor via UDP
    private Animator animator;
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private SpriteRenderer scissorsRenderer;

    public enum ControlMode
    {
        Auto,        // Singleplayer: normal InputSystem. Multiplayer: Player 1 -> WASD, Player 2 -> Arrows
        WASD_Only,   // Strictly W, A, S, D (+ Arduino sensor)
        Arrows_Only  // Strictly Up, Down, Left, Right arrows
    }

    [Header("Control Scheme")]
    [SerializeField] private ControlMode controlMode = ControlMode.Auto;
    private ControlMode resolvedMode;

    public void UpgradeSpeed(float mult)
    {
        moveSpeed *= mult;
    }

    void Awake()
    {
        // Читаем по новому ключу
        moveSpeed = PlayerPrefs.GetFloat("Speed_Player", 120f);
        Debug.Log("<color=cyan>СКОРОСТЬ ИГРОКА ЗАГРУЖЕНА: " + moveSpeed + "</color>");

        bool isMultiplayer = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SampleSceneMultiplayer";
        if (controlMode == ControlMode.Auto)
        {
            if (isMultiplayer)
            {
                resolvedMode = (gameObject.name.Contains("2") || gameObject.name.Contains("Wolf")) ? ControlMode.Arrows_Only : ControlMode.WASD_Only;
            }
            else
            {
                resolvedMode = ControlMode.Auto;
            }
        }
        else
        {
            resolvedMode = controlMode;
        }

        rb = GetComponent<Rigidbody2D>();
        rb.linearDamping = drag;
        QualitySettings.vSyncCount = 1;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static bool IsWinKeyDown(int vKey)
    {
        return (GetAsyncKeyState(vKey) & 0x8000) != 0;
    }
#else
    private static bool IsWinKeyDown(int vKey) => false;
#endif

    private const int VK_LEFT  = 0x25;
    private const int VK_UP    = 0x26;
    private const int VK_RIGHT = 0x27;
    private const int VK_DOWN  = 0x28;
    private const int VK_W     = 0x57;
    private const int VK_A     = 0x41;
    private const int VK_S     = 0x53;
    private const int VK_D     = 0x44;

    void Start()
    {
        // Player 1 (and Singleplayer) listens on port 5005. Player 2 (Arrows_Only / Wolf) listens on port 5006.
        int targetPort = (resolvedMode == ControlMode.Arrows_Only) ? 5006 : 5005;
        var sensor = GetComponent<ArduinoSensorInput>();
        if (sensor == null)
        {
            sensor = gameObject.AddComponent<ArduinoSensorInput>();
        }
        sensor.SetListenPort(targetPort);
    }

    void Update()
    {
        float x = 0f;
        float y = 0f;
        bool hasDirectInput = false;

        // Check WASD keys (used in WASD_Only and in Auto/Singleplayer)
        if (resolvedMode == ControlMode.WASD_Only || resolvedMode == ControlMode.Auto)
        {
            bool w = (Keyboard.current != null && Keyboard.current.wKey.isPressed) || IsWinKeyDown(VK_W);
            bool s = (Keyboard.current != null && Keyboard.current.sKey.isPressed) || IsWinKeyDown(VK_S);
            bool a = (Keyboard.current != null && Keyboard.current.aKey.isPressed) || IsWinKeyDown(VK_A);
            bool d = (Keyboard.current != null && Keyboard.current.dKey.isPressed) || IsWinKeyDown(VK_D);

            if (w) y += 1f;
            if (s) y -= 1f;
            if (a) x -= 1f;
            if (d) x += 1f;

            if (w || s || a || d)
                hasDirectInput = true;
        }

        // Check Arrow keys (used in Arrows_Only, and in Auto/Singleplayer if WASD is not currently pressed)
        if (resolvedMode == ControlMode.Arrows_Only || (resolvedMode == ControlMode.Auto && !hasDirectInput))
        {
            bool up = (Keyboard.current != null && Keyboard.current.upArrowKey.isPressed) || IsWinKeyDown(VK_UP);
            bool down = (Keyboard.current != null && Keyboard.current.downArrowKey.isPressed) || IsWinKeyDown(VK_DOWN);
            bool left = (Keyboard.current != null && Keyboard.current.leftArrowKey.isPressed) || IsWinKeyDown(VK_LEFT);
            bool right = (Keyboard.current != null && Keyboard.current.rightArrowKey.isPressed) || IsWinKeyDown(VK_RIGHT);

            if (up) y += 1f;
            if (down) y -= 1f;
            if (left) x -= 1f;
            if (right) x += 1f;

            if (up || down || left || right)
                hasDirectInput = true;
        }

        if (hasDirectInput)
        {
            Vector2 raw = new Vector2(x, y);
            moveInput = raw.sqrMagnitude > 1f ? raw.normalized : raw;
        }
        else if (resolvedMode != ControlMode.Auto)
        {
            moveInput = Vector2.zero;
        }
        // In Auto mode, if no keyboard/simulated key is held, moveInput remains whatever OnMove() provided (gamepad stick, etc.)
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (resolvedMode == ControlMode.Auto)
        {
            moveInput = context.ReadValue<Vector2>();
        }
    }

    /// <summary>
    /// Called by ArduinoSensorInput to inject movement directly from the gyroscope,
    /// bypassing the unreliable SendInput → Unity Input System pipeline.
    /// </summary>
    public void SetSensorInput(Vector2 input)
    {
        sensorInput = input;
    }

    void FixedUpdate()
    {
        // Whichever input source is stronger (keyboard 1.0 or sensor analog) wins
        Vector2 effectiveInput = moveInput.sqrMagnitude >= sensorInput.sqrMagnitude ? moveInput : sensorInput;

        if (animator != null)
        {
            animator.SetFloat("MoveX", effectiveInput.x);
            animator.SetFloat("MoveY", effectiveInput.y);
            animator.SetFloat("Speed", effectiveInput.magnitude);

            if (effectiveInput.x != 0 || effectiveInput.y != 0)
            {
                animator.SetFloat("LookX", effectiveInput.x);
                animator.SetFloat("LookY", effectiveInput.y);
            }
        }

        if (rb != null)
        {
            rb.AddForce(effectiveInput * moveSpeed);
        }
    }
}