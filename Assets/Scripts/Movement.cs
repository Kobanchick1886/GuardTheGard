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
                resolvedMode = gameObject.name.Contains("2") ? ControlMode.Arrows_Only : ControlMode.WASD_Only;
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

    void Update()
    {
        if (resolvedMode == ControlMode.WASD_Only)
        {
            float x = 0f;
            float y = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) y += 1f;
                if (Keyboard.current.sKey.isPressed) y -= 1f;
                if (Keyboard.current.aKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed) x += 1f;
            }
            Vector2 raw = new Vector2(x, y);
            moveInput = raw.sqrMagnitude > 1f ? raw.normalized : raw;
        }
        else if (resolvedMode == ControlMode.Arrows_Only)
        {
            float x = 0f;
            float y = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.isPressed) y += 1f;
                if (Keyboard.current.downArrowKey.isPressed) y -= 1f;
                if (Keyboard.current.leftArrowKey.isPressed) x -= 1f;
                if (Keyboard.current.rightArrowKey.isPressed) x += 1f;
            }
            Vector2 raw = new Vector2(x, y);
            moveInput = raw.sqrMagnitude > 1f ? raw.normalized : raw;
        }
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
        // Use keyboard/gamepad input if available, otherwise fall back to sensor
        Vector2 effectiveInput = moveInput.sqrMagnitude > 0.001f ? moveInput : sensorInput;

        animator.SetFloat("MoveX", effectiveInput.x);
        animator.SetFloat("MoveY", effectiveInput.y);
        animator.SetFloat("Speed", effectiveInput.magnitude);

        if (effectiveInput.x != 0 || effectiveInput.y != 0)
        {
            animator.SetFloat("LookX", effectiveInput.x);
            animator.SetFloat("LookY", effectiveInput.y);
        }

        rb.AddForce(effectiveInput * moveSpeed); 

    }
}