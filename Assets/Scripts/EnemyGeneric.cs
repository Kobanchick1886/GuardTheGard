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

    void Awake()
    {
        root = GetComponentsInChildren<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        objective = GameObject.FindWithTag("Objective");
        player = GameObject.FindWithTag("Player");
        counter = Object.FindAnyObjectByType<objective>();

        foreach (BoxCollider2D box in root)
        {
            if (box != null) box.GetComponent<SpriteRenderer>().enabled = false;
            box.enabled = false;
        }
    }

    void Update()
    {
        // Если все корни уничтожены — умираем
        if (transform.childCount == 0)
        {
            Die();
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