using System;
using System.Collections;
using UnityEngine;

public class LawnMower : MonoBehaviour
{
    [System.Serializable]
    public struct BaseSpritePair
    {
        public Sprite background; // Çàäí³é øàð
        public Sprite foreground; // Ïåðåäí³é øàð
        public Vector2 foregroundOffset;
    }

    [Header("Prefabs & Player")]
    [SerializeField] private GameObject mowerPrefab;
    [SerializeField] private GameObject basePrefab;

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

    private Transform player;
    private GameObject spawnedMower;
    private GameObject spawnedBase1;
    private GameObject spawnedBase2;

    private float mowerSpeed = 5f;

    [HideInInspector] public float countdownTimer = 0f;
    [HideInInspector] public bool isPlacing = false;

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

        // Fase 1
        countdownTimer = 5f;
        while (countdownTimer > 0)
        {
            countdownTimer -= Time.deltaTime;
            yield return null;
        }

        spawnedBase1 = Instantiate(basePrefab, player.position, Quaternion.identity);
        SetSingleBaseSprite(spawnedBase1, verticalBaseDown);

        // Fase 2
        countdownTimer = 5f;
        while (countdownTimer > 0)
        {
            countdownTimer -= Time.deltaTime;
            yield return null;
        }

        spawnedBase2 = Instantiate(basePrefab, player.position, Quaternion.identity);

        // Fase 3
        Vector3 pos1 = spawnedBase1.transform.position;
        Vector3 pos2 = spawnedBase2.transform.position;

        Vector3 direction = (pos2 - pos1).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        SetupBaseSprites(spawnedBase1, spawnedBase2, angle);

        spawnedMower = Instantiate(mowerPrefab, pos1, Quaternion.identity);

        isPlacing = false;

        MowerItself mowerScript = spawnedMower.GetComponent<MowerItself>();
        if (mowerScript != null)
        {
            mowerScript.StartPatrol(pos1, pos2, mowerSpeed);
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
            Debug.LogError("Áàçà ïîâèííà ìàòè ÿê ì³í³ìóì 2 äî÷³ðí³ SpriteRenderer (Back òà Front)!");
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

        // ÂÅÐÒÈÊÀËÜÍ² ÁÀÇÈ
        if ((angle > 67.5f && angle < 112.5f) || (angle < -67.5f && angle > -112.5f))
        {
            // angle > 0 îçíà÷àº ðóõ çíèçó âãîðó Base1 - çíèçó
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
        // ÃÎÐÈÇÎÍÒÀËÜÍ² ÁÀÇÈ
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
        // Ä²ÀÃÎÍÀËÜ UP-RIGHT
        else if (angle >= 22.5f && angle <= 67.5f)
        {
            // Base1 - íèç
            ApplyIndividualPair(diagDownLeftBase, back1, front1);
            ApplyIndividualPair(diagUpLeftBase, back2, front2);

            SetFlipX(true, back1, front1);
        }
        // UP-LEFT
        else if (angle > 112.5f && angle < 157.5f)
        {
            // Base1 - íèç
            ApplyIndividualPair(diagDownLeftBase, back1, front1);
            ApplyIndividualPair(diagUpLeftBase, back2, front2);

            SetFlipX(true, back2, front2);
        }
        // DOWN-RIGHT
        else if (angle > -67.5f && angle < -22.5f)
        {
            // Base1 - âåðõ
            ApplyIndividualPair(diagUpLeftBase, back1, front1);
            ApplyIndividualPair(diagDownLeftBase, back2, front2);

            SetFlipX(true, back2, front2);
        }
        // DOWN-LEFT
        else if (angle >= -157.5f && angle <= -112.5f)
        {
            // Base1 - âåðõ
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