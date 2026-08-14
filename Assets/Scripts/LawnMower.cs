using System;
using System.Collections;
using UnityEngine;

public class LawnMower : MonoBehaviour
{
    [System.Serializable]
    public struct BaseSpritePair
    {
        public Sprite background; // Задній шар
        public Sprite foreground; // Передній шар
        public Vector2 foregroundOffset;
    }

    [Header("Prefabs & Player")]
    [SerializeField] private GameObject mowerPrefab;
    [SerializeField] private GameObject basePrefab;

    [Header("Hover Icon (Placement preview)")]
    [Tooltip("Спрайт зеленой косилки (путь свободен)")]
    [SerializeField] private Sprite iconClear;
    [Tooltip("Спрайт красной косилки (путь заблокирован)")]
    [SerializeField] private Sprite iconBlocked;
    [Tooltip("Отступ для иконки косилки от игрока")]
    [SerializeField] private Vector3 hoverIconOffset = new Vector3(0, 1.5f, 0);

    [Header("Two-Layer Base Sprites")]
    [Tooltip("DOWN")]
    [SerializeField] private BaseSpritePair verticalBaseDown;

    [Tooltip("UP")]
    [SerializeField] private BaseSpritePair verticalBaseUp;

    [Tooltip("RIGHT-LEFT")]
    [SerializeField] private BaseSpritePair horizontalBase;

    [Tooltip("UP-LEFT")]
    [SerializeField] private BaseSpritePair diagUpLeftBase;

    [Tooltip("DOWN-LEFT")]
    [SerializeField] private BaseSpritePair diagDownLeftBase;

    [Header("Obstacle Detection")]
    [Tooltip("Тег главного куста, который блокирует траекторию")]
    [SerializeField] private string obstacleTag = "Objective";

    private Transform player;
    private GameObject spawnedMower;
    private GameObject spawnedBase1;
    private GameObject spawnedBase2;

    // Переменные для визуала над головой (теперь только иконка)
    private GameObject currentHoverIcon;
    private SpriteRenderer hoverIconRenderer;

    public float mowerSpeed = 5f;

    [HideInInspector] public float countdownTimer = 0f;
    [HideInInspector] public bool isPlacing = false;
    [HideInInspector] public bool isPathBlocked = false;

    void Awake()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    private void Start()
    {
        StartCoroutine(PowerUpSequence());
    }

    public void StartPowerUp()
    {
        StartCoroutine(PowerUpSequence());
    }

    private IEnumerator PowerUpSequence()
    {
        isPlacing = true;
        isPathBlocked = false;

        // 1. Динамически создаем объект для иконки косилки
        currentHoverIcon = new GameObject("StylizedHoverIcon");
        hoverIconRenderer = currentHoverIcon.AddComponent<SpriteRenderer>();
        hoverIconRenderer.sortingOrder = 99;

        // Fase 1 - Установка первой базы (отсчет 3 секунды)
        countdownTimer = 3f;
        while (countdownTimer > 0)
        {
            countdownTimer -= Time.deltaTime;

            UpdateHoverIconPosition();
            UpdateHoverIconSprite();

            yield return null;
        }

        spawnedBase1 = Instantiate(basePrefab, player.position, Quaternion.identity);
        SetSingleBaseSprite(spawnedBase1, verticalBaseDown);

        Vector3 pos1 = spawnedBase1.transform.position;

        // Fase 2 - Установка второй базы 
        countdownTimer = 3f;
        while (countdownTimer > 0)
        {
            Vector3 currentPos = player.position;
            bool pathClear = true;

            // Пускаем луч от первой базы к игроку
            RaycastHit2D[] hits = Physics2D.LinecastAll(pos1, currentPos);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.CompareTag(obstacleTag))
                {
                    pathClear = false;
                    break; // Как только наткнулись на куст - путь заблокирован
                }
            }

            isPathBlocked = !pathClear;

            // ТАЙМЕР ИДЕТ ТОЛЬКО ЕСЛИ ПУТЬ ЧИСТ
            if (pathClear)
            {
                countdownTimer -= Time.deltaTime;
            }

            UpdateHoverIconPosition();
            UpdateHoverIconSprite();

            yield return null;
        }

        Vector3 finalPos2 = player.position;

        // Удаляем иконку косилки, когда таймер вышел
        if (currentHoverIcon != null) Destroy(currentHoverIcon);

        // Fase 3 - Размещение и запуск газонокосилки
        spawnedBase2 = Instantiate(basePrefab, finalPos2, Quaternion.identity);

        Vector3 pos2 = spawnedBase2.transform.position;
        Vector3 direction = (pos2 - pos1).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        SetupBaseSprites(spawnedBase1, spawnedBase2, angle);

        spawnedMower = Instantiate(mowerPrefab, pos1, Quaternion.identity);

        isPlacing = false;
        isPathBlocked = false;

        MowerItself mowerScript = spawnedMower.GetComponent<MowerItself>();
        if (mowerScript != null)
        {
            mowerScript.StartPatrol(pos1, pos2, mowerSpeed);
        }
    }

    private void UpdateHoverIconPosition()
    {
        if (currentHoverIcon != null)
        {
            currentHoverIcon.transform.position = player.position + hoverIconOffset;
        }
    }

    private void UpdateHoverIconSprite()
    {
        if (hoverIconRenderer != null)
        {
            hoverIconRenderer.sprite = isPathBlocked ? iconBlocked : iconClear;
        }
    }

    private void SetSingleBaseSprite(GameObject baseObj, BaseSpritePair pair)
    {
        SpriteRenderer[] renderers = baseObj.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length >= 2)
        {
            renderers[0].sprite = pair.background;
            renderers[1].sprite = pair.foreground;

            renderers[0].sortingOrder = 1;
            renderers[1].sortingOrder = 25;

            renderers[1].transform.localPosition = pair.foregroundOffset;
        }
    }

    private void SetupBaseSprites(GameObject base1, GameObject base2, float angle)
    {
        SpriteRenderer[] renderers1 = base1.GetComponentsInChildren<SpriteRenderer>();
        SpriteRenderer[] renderers2 = base2.GetComponentsInChildren<SpriteRenderer>();

        if (renderers1.Length < 2 || renderers2.Length < 2)
        {
            Debug.LogError("База повинна мати як мінімум 2 дочірні SpriteRenderer (Back та Front)!");
            return;
        }

        SpriteRenderer back1 = renderers1[0];
        SpriteRenderer front1 = renderers1[1];
        SpriteRenderer back2 = renderers2[0];
        SpriteRenderer front2 = renderers2[1];

        back1.sortingOrder = 1;
        back2.sortingOrder = 1;
        front1.sortingOrder = 25;
        front2.sortingOrder = 25;

        ResetFlips(back1, front1, back2, front2);

        if ((angle > 67.5f && angle < 112.5f) || (angle < -67.5f && angle > -112.5f))
        {
            if (angle > 0)
            {
                ApplyIndividualPair(verticalBaseDown, back1, front1);
                ApplyIndividualPair(verticalBaseUp, back2, front2);
            }
            else
            {
                ApplyIndividualPair(verticalBaseUp, back1, front1);
                ApplyIndividualPair(verticalBaseDown, back2, front2);
            }
        }
        else if (angle >= 157.5f || angle <= -157.5f || (angle >= -22.5f && angle <= 22.5f))
        {
            ApplyIndividualPair(horizontalBase, back1, front1);
            ApplyIndividualPair(horizontalBase, back2, front2);

            if (angle >= -22.5f && angle <= 22.5f)
            {
                SetFlipX(true, back2, front2);
            }
            else
            {
                SetFlipX(true, back1, front1);
            }
        }
        else if (angle >= 22.5f && angle <= 67.5f)
        {
            ApplyIndividualPair(diagDownLeftBase, back1, front1);
            ApplyIndividualPair(diagUpLeftBase, back2, front2);
            SetFlipX(true, back2, front2);
        }
        else if (angle > 112.5f && angle < 157.5f)
        {
            ApplyIndividualPair(diagDownLeftBase, back1, front1);
            ApplyIndividualPair(diagUpLeftBase, back2, front2);
            SetFlipX(true, back1, front1);
        }
        else if (angle > -67.5f && angle < -22.5f)
        {
            ApplyIndividualPair(diagUpLeftBase, back1, front1);
            ApplyIndividualPair(diagDownLeftBase, back2, front2);
            SetFlipX(true, back2, front2);
        }
        else if (angle >= -157.5f && angle <= -112.5f)
        {
            ApplyIndividualPair(diagUpLeftBase, back1, front1);
            ApplyIndividualPair(diagDownLeftBase, back2, front2);
            SetFlipX(true, back1, front1);
        }
    }

    private void ApplyIndividualPair(BaseSpritePair pair, SpriteRenderer back, SpriteRenderer front)
    {
        back.sprite = pair.background;
        front.sprite = pair.foreground;
        back.transform.localPosition = Vector3.zero;
        front.transform.localPosition = pair.foregroundOffset;
    }

    private void ResetFlips(params SpriteRenderer[] renderers)
    {
        foreach (var r in renderers) { r.flipX = false; r.flipY = false; }
    }

    private void SetFlipX(bool state, params SpriteRenderer[] renderers)
    {
        foreach (var r in renderers) r.flipX = state;
    }

    private void SetFlipY(bool state, params SpriteRenderer[] renderers)
    {
        foreach (var r in renderers) r.flipY = state;
    }
}