using System.Collections;
using UnityEngine;

public class EnemyGeneric : MonoBehaviour
{
    private GameObject player;
    private Vector2 playerPos;
    private float distance;
    private float distanceToPlayer;
    private Vector3 direction;
    private Vector3 directionFromPlayer;
    private Vector3 finalDirection;
    public float moveSpeed = 8f;
    public float detectionRange = 10f;
    private float avoidanceWeight = 0.8f;
    private objective counter;
    private GameObject objective;
    private Rigidbody2D rb;
    private bool isMoving = true;
    private BoxCollider2D[] root;

    // Флаг для предотвращения множественных смертей
    private bool isDead = false;

    [SerializeField]
    private bool Marker;

    //для анімацій
    [Header("Animation Settings")]
    public Transform legsTransform;
    private Animator animator;

    void Awake()
    {
        root = GetComponentsInChildren<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        objective = GameObject.FindWithTag("Objective");
        player = GameObject.FindWithTag("Player");
        counter = Object.FindAnyObjectByType<objective>();

        animator = GetComponent<Animator>();

        foreach (BoxCollider2D box in root)
        {
            if (box != null) box.GetComponent<SpriteRenderer>().enabled = false;
            box.enabled = false;
        }
    }

    void Update()
    {

        if (Marker)
        {
            bool hasRootsLeft = false;

            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("root_0"))
                {
                    hasRootsLeft = true;
                    break;
                }
            }

            if (!hasRootsLeft)
            {
                Die();
            }

        }
    }

    void Die()
    {
        // Если враг уже начал процесс смерти — игнорируем новые вызовы
        if (isDead) return;

        isDead = true; // Сразу ставим флаг
        counter.CountEnemyDeath();
        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        // Останавливаем логику движения, если враг уже мертв
        if (isDead) return;

        playerPos = player.transform.position;
        distance = Vector2.Distance(transform.position, objective.transform.position);
        distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        direction = (objective.transform.position - transform.position).normalized;

        if (isMoving)
        {
            if (distanceToPlayer < detectionRange)
            {
                directionFromPlayer = (transform.position - player.transform.position).normalized;
            }
            if (distanceToPlayer > 10f)
            {
                directionFromPlayer = Vector3.zero;
            }
            finalDirection = direction + (directionFromPlayer * avoidanceWeight);
            rb.linearVelocity = (finalDirection * moveSpeed);

            HandleVisuals(finalDirection);
        }

        if (animator != null)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "isMoving")
                {
                    animator.SetBool("isMoving", isMoving);
                    break;
                }
            }
        }

    }

    //для фліпа й анімації ніг
    private void HandleVisuals(Vector2 moveDir)
    {
        if (moveDir.sqrMagnitude < 0.01f) return;
        bool isFlippedLeft = false;
        if (moveDir.x < 0)
        {
            transform.localScale = new Vector3(-0.6f, 0.6f, 0.6f);
            isFlippedLeft = true;
        }
        else if (moveDir.x > 0)
        {
            transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            isFlippedLeft = false;
        }
        if (legsTransform != null)
        {
            float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            if (isFlippedLeft)
            {
                angle = 180f - angle;
            }
            else
            {
                angle = angle + 0f;
            }
            legsTransform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }




    public IEnumerator Stun()
    {
        if (!Marker)
        {
            Die();
            yield break;
        }

        isMoving = false;
        foreach (BoxCollider2D box in root)
        {
            if (box != null)
            {
                box.GetComponent<SpriteRenderer>().enabled = true;
                box.enabled = true;
            }
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;

        yield return new WaitForSeconds(5);

        // Если врага убили за время стана, прерываем корутину, чтобы не было ошибок
        if (isDead) yield break;

        rb.bodyType = RigidbodyType2D.Dynamic;

        foreach (BoxCollider2D box in root)
        {
            if (box != null)
            {
                box.GetComponent<SpriteRenderer>().enabled = false;
                box.enabled = false;
            }
        }
        isMoving = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag("Player") && !isMoving)
        {
            foreach (BoxCollider2D box in root)
            {
                if (box != null && box.enabled)
                {
                    if (box.bounds.Intersects(other.bounds))
                    {
                        Destroy(box.gameObject);
                    }
                }
            }
        }
    }
}