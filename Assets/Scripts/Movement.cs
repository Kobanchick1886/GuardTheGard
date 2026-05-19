using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Movement : MonoBehaviour
{
    private float moveSpeed = 120f;
    private float drag = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    public void UpgradeSpeed(float mult)
    {
        moveSpeed *= mult;
    }

    void Awake()
    {
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

        rb.AddForce(moveInput * moveSpeed);

        if (moveInput.x < 0)
        {
            transform.localScale = new Vector3(-0.5f, 0.5f, 0.5f);
        }
        else if (moveInput.x > 0)
        {
            transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }

    }
}