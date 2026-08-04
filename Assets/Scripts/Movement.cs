using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 120f;
    private float drag = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private SpriteRenderer scissorsRenderer;

    public void UpgradeSpeed(float mult)
    {
        moveSpeed *= mult;
    }

    void Awake()
    {
        // Читаем по новому ключу
        moveSpeed = PlayerPrefs.GetFloat("Speed_Player", 120f);
        Debug.Log("<color=cyan>СКОРОСТЬ ИГРОКА ЗАГРУЖЕНА: " + moveSpeed + "</color>");

        rb = GetComponent<Rigidbody2D>();
        rb.linearDamping = drag;
        QualitySettings.vSyncCount = 1;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
 
    void FixedUpdate()
    {
        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);
        animator.SetFloat("Speed", moveInput.magnitude);

        if (moveInput.x != 0 || moveInput.y != 0)
        {
            animator.SetFloat("LookX", moveInput.x);
            animator.SetFloat("LookY", moveInput.y);
        }

        rb.AddForce(moveInput * moveSpeed); 

    }
}