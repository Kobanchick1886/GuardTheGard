using System.Collections;
using UnityEngine;

public class MowerItself : MonoBehaviour
{
    public int smashedEnemies = 0;

    [Header("Animation & Base Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float slowDownDistance = 2.0f;

    [Header("Drive Out & Spawn Settings")]
    [Tooltip("Відстань від бази, з якої починає рух")]
    [SerializeField] private float initialSpawnOffset = 0.8f;

    [Tooltip("Додаткова відстань, на яку косарка від'їде задом при паркуванні")]
    [SerializeField] private float extraDriveOutDistance = 1.0f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource mowerAudioSource;
    [SerializeField] private AudioClip mowerEngineSound;
    [SerializeField] private float minDistance = 2f;   
    [SerializeField] private float maxDistance = 15f;      

    private Vector3 pointA;
    private Vector3 pointB;
    private float moveSpeed;
    private bool isPatrolling = false;
    private Rigidbody2D rb;

    private string currentAnimStep = "";

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        SetupAndPlayEngineAudio();
    }

    private void SetupAndPlayEngineAudio()
    {
        // Якщо AudioSource не призначений в інспекторі, шукаємо або додаємо його
        if (mowerAudioSource == null)
        {
            mowerAudioSource = GetComponent<AudioSource>();
            if (mowerAudioSource == null)
            {
                mowerAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (mowerEngineSound != null)
        {
            mowerAudioSource.clip = mowerEngineSound;
            mowerAudioSource.loop = true; // Зациклюємо звук
            mowerAudioSource.playOnAwake = true;

            // Налаштування 3D-звуку для залежності від відстані
            mowerAudioSource.spatialBlend = 1.0f; // 1.0 = 3D звук (реагує на відстань)
            mowerAudioSource.rolloffMode = AudioRolloffMode.Logarithmic; // або Linear
            mowerAudioSource.minDistance = minDistance; // Повна гучність у межах цієї відстані
            mowerAudioSource.maxDistance = maxDistance; // Межа чутливості

            mowerAudioSource.Play();
        }
    }

    public void OnAnimationStep(string stepName)
    {
        currentAnimStep = stepName;
    }

    public void StartPatrol(Vector3 start, Vector3 end, float speed)
    {
        //start = початкова база, end = друга база
        pointA = start;
        pointB = end;
        moveSpeed = speed;

        Vector3 dirToTarget = (pointB - pointA).normalized;
        transform.position = pointA + dirToTarget * initialSpawnOffset;

        isPatrolling = true;
        StartCoroutine(PatrolRoutine());
    }

    private IEnumerator PatrolRoutine()
    {
        while (isPatrolling)
        {
            // від а до в
            Vector3 dirAtoB = (pointB - pointA).normalized;
            PlayMovementAnimation(dirAtoB, isDocking: false);

            while (Vector3.Distance(transform.position, pointB) > slowDownDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, pointB, moveSpeed * Time.deltaTime);
                yield return null;
            }

            // паркування в
            PlayMovementAnimation(dirAtoB, isDocking: true);
            yield return PlayDockingSequence(pointB);


            // від в до а
            Vector3 dirBtoA = (pointA - pointB).normalized;
            PlayMovementAnimation(dirBtoA, isDocking: false);

            while (Vector3.Distance(transform.position, pointA) > slowDownDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, pointA, moveSpeed * Time.deltaTime);
                yield return null;
            }

            // паркування а
            PlayMovementAnimation(dirBtoA, isDocking: true);
            yield return PlayDockingSequence(pointA);
        }
    }

    private void PlayMovementAnimation(Vector3 direction, bool isDocking)
    {
        if (animator == null || spriteRenderer == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        spriteRenderer.flipX = false;
        spriteRenderer.flipY = false;

        string animToPlay = "";

        // ВЕРТИКАЛЬНИЙ РУХ
        if (angle > 67.5f && angle < 112.5f) // UP
        {
            animToPlay = isDocking ? "LM-docking-up" : "LM-drive-up";
        }
        else if (angle < -67.5f && angle > -112.5f) // DOWN
        {
            animToPlay = isDocking ? "LM-docking-down" : "LM-drive-down";
        }

        // ГОРИЗОНТАЛЬНИЙ РУХ
        else if (angle >= 157.5f || angle <= -157.5f) // LEFT
        {
            animToPlay = isDocking ? "LM-docking-left" : "LM-drive-left";
        }
        else if (angle >= -22.5f && angle <= 22.5f) // RIGHT - flip
        {
            animToPlay = isDocking ? "LM-docking-left" : "LM-drive-left";
            spriteRenderer.flipX = true;
        }

        // ДІАГОНАЛЬНИЙ РУХ
        else if (angle >= 22.5f && angle <= 67.5f) // UP-RIGHT
        {
            animToPlay = isDocking ? "LM-docking-up-right" : "LM-drive-up-right";
        }
        else if (angle > 112.5f && angle < 157.5f) // UP-LEFT - flip x
        {
            animToPlay = isDocking ? "LM-docking-up-right" : "LM-drive-up-right";
            spriteRenderer.flipX = true;
        }
        else if (angle >= -157.5f && angle <= -112.5f) // DOWN-LEFT
        {
            animToPlay = isDocking ? "LLM-docking-down-left" : "LM-drive-down-left";
        }
        else if (angle > -67.5f && angle < -22.5f) // DOWN-RIGHT - flip x
        {
            animToPlay = isDocking ? "LLM-docking-down-left" : "LM-drive-down-left";
            spriteRenderer.flipX = true;
        }

        if (!string.IsNullOrEmpty(animToPlay))
        {
            animator.SetBool("IsDocking", isDocking);
            animator.Play(animToPlay, 0, 0f);
        }
    }

    private IEnumerator PlayDockingSequence(Vector3 targetBase)
    {
        Vector3 dockStartPoint = transform.position;
        Vector3 dirFromBase = (dockStartPoint - targetBase).normalized;
        Vector3 driveOutPoint = dockStartPoint + (dirFromBase * extraDriveOutDistance);

        currentAnimStep = "";

        float totalDockDistance = Vector3.Distance(dockStartPoint, targetBase);

        while (currentAnimStep != "BaseReached" && Vector3.Distance(transform.position, targetBase) > 0.02f)
        {
            float currentDist = Vector3.Distance(transform.position, targetBase);
            float t = totalDockDistance > 0f ? currentDist / totalDockDistance : 0f;
            t = Mathf.Clamp01(t);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float easeFactor = Mathf.Lerp(0.5f, 1.0f, smoothT);
            float currentSpeed = moveSpeed * easeFactor;

            transform.position = Vector3.MoveTowards(transform.position, targetBase, currentSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetBase;

        // ПАУЗА - 2
        float waitTimer = 0f;
        while (currentAnimStep != "DriveOutStart" && waitTimer < 0.6f)
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }

        // ВИЇЗД ЗАДОМ -3
        float reverseSpeed = moveSpeed * 2f;

        while (Vector3.Distance(transform.position, driveOutPoint) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, driveOutPoint, reverseSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = driveOutPoint; // Фіксуємо позицію у новій точці

        // ПАУЗА/РОЗВОРІТ - 4
        waitTimer = 0f;
        while (currentAnimStep != "DockingFinished" && waitTimer < 0.5f)
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            smashedEnemies++;
            var objScript = FindFirstObjectByType<objective>();
            if (objScript != null) objScript.CountEnemyDeath();
            Destroy(collision.gameObject);
        }
    }
}