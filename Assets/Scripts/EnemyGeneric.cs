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
    public bool Marker;
    public bool IsStunned()
    {
        return !isMoving || isDead;
    }
    //для анімацій переміщення ворогів
    [Header("Animation Settings")]
    public Transform legsTransform;
    private Animator animator;

    //для анімації помирання ворогів
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeathHash = Animator.StringToHash("Die");


    void Awake()
    {
        // Читаем по новому ключу
        moveSpeed = PlayerPrefs.GetFloat("Speed_Enemy", 8f);
        Debug.Log("<color=red>СКОРОСТЬ ВРАГА ЗАГРУЖЕНА: " + moveSpeed + "</color>");

        root = GetComponentsInChildren<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        objective = GameObject.FindWithTag("Objective");
        player = GameObject.FindWithTag("Player");
        counter = Object.FindAnyObjectByType<objective>();

        animator = GetComponent<Animator>();

        HideRoots();
    }

    void Update()
    {

        if (Marker && !isDead)
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

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        isMoving = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        HideRoots();

        if (counter != null)
        {
            counter.CountEnemyDeath();
        }
        StartCoroutine(PlayDeathAndDestroy());
    }

    private IEnumerator PlayDeathAndDestroy()
    {
        if (animator != null)
        {
            animator.ResetTrigger(HitHash);
            animator.SetTrigger(DeathHash);

            int initialStateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            float safetyTime = 0.3f;

            while (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == initialStateHash && safetyTime > 0)
            {
                safetyTime -= Time.deltaTime;
                yield return null;
            }

            while (animator.IsInTransition(0))
            {
                yield return null;
            }

            AnimatorStateInfo deathState = animator.GetCurrentAnimatorStateInfo(0);
            float remainingTime = deathState.length * (1f - (deathState.normalizedTime % 1f));

            if (remainingTime > 0f)
            {
                yield return new WaitForSeconds(remainingTime);
            }
        }
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
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (animator != null)
        {
            animator.SetBool(IsMovingHash, isMoving);
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
       
        if (isDead) yield break;

        isMoving = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;


        if (animator != null)
        {
            animator.SetTrigger(HitHash);
        }

        StartCoroutine(SmoothResetLegsRotation());

        if (Marker)
        {
            yield return new WaitForSeconds(5f);

            // Если врага убили за время стана, прерываем корутину, чтобы не было ошибок
            if (isDead) yield break;

            rb.bodyType = RigidbodyType2D.Dynamic;
            HideRoots();
            isMoving = true;
        }
    }

    private IEnumerator SmoothResetLegsRotation()
    {
        if (legsTransform == null) yield break;

        yield return null;

        float duration = 0.3f;
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            duration = stateInfo.length;
        }

        Quaternion startRotation = legsTransform.localRotation;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (isDead) yield break;

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            // Сферична інтерполяція
            legsTransform.localRotation = Quaternion.Slerp(startRotation, Quaternion.identity, t);

            yield return null;
        }

        legsTransform.localRotation = Quaternion.identity;
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
                        box.enabled = false;

                        Animator rootAnimator = box.GetComponent<Animator>();

                        if (rootAnimator != null)
                        {
                            StartCoroutine(PlayDigAndDestroy(rootAnimator, box.gameObject));
                        }
                        else
                        {
                            Destroy(box.gameObject);
                        }
                    }
                }
            }
        }
    }

    //для анімації викопуванняя
    private IEnumerator PlayDigAndDestroy(Animator rootAnim, GameObject rootGO)
    {
        if (rootAnim != null)
        {
            rootAnim.Play("Root-dig", 0, 0f);

            yield return null;

            AnimatorStateInfo stateInfo = rootAnim.GetCurrentAnimatorStateInfo(0);
            float animLength = stateInfo.length / Mathf.Max(rootAnim.speed, 0.1f);

            yield return new WaitForSeconds(animLength);
        }

        if (rootGO != null)
        {
            Destroy(rootGO);
        }
    }

    public void OnHitAnimationEnd()
    {
        if (isDead) return;

        if (Marker)
        {
            ShowRoots();
        }
        else
        {
            Die();
        }
    }

    private void ShowRoots()
    {
        foreach (BoxCollider2D box in root)
        {
            if (box != null)
            {
                var sr = box.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = true;
                box.enabled = true;
            }
        }
    }

    private void HideRoots()
    {
        foreach (BoxCollider2D box in root)
        {
            if (box != null)
            {
                var sr = box.GetComponent<SpriteRenderer>();
                if (sr != null ) sr.enabled = false;
                box.enabled = false;
            }
        }
    }
}