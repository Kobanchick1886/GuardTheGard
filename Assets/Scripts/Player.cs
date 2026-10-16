using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class Player : MonoBehaviour
{
    private Movement _movement;

    [Header("Player Identity")]
    [Tooltip("1 for Player 1 (WASD), 2 for Player 2 (Arrows). If 0, auto-detected from GameObject name.")]
    public int playerIndex = 0;

    [Header("EXP Inventory")]
    [SerializeField] private int EXP = 0;

    [Header("UI")]
    [Tooltip("TextMeshPro component displaying remaining spray charges for this player.")]
    public TextMeshProUGUI expText;

    [Tooltip("Alias for expText (if assigned, will be synchronized with expText)")]
    public TextMeshProUGUI chargesText;

    [Header("Multiplayer Sharing Settings")]
    [Tooltip("Small radius of the interaction trigger collider for player-to-player sharing.")]
    [SerializeField] public float sharingColliderRadius = 1.5f;
    [SerializeField] public AudioClip transferSound;

    // --- Statistics for CSV session logging ---
    public int ExpCollected { get; private set; } = 0;
    public int ExpGiven { get; private set; } = 0;
    public int CurrentExp => EXP;

    public int TotalResources => RemainingCharges + EXP;

    private Magnet _magnet;
    public Magnet MagnetComponent
    {
        get
        {
            if (_magnet == null)
            {
                _magnet = GetComponentInChildren<Magnet>(true);
            }
            return _magnet;
        }
    }

    public int RemainingCharges => MagnetComponent != null ? MagnetComponent.currentCharges : 0;

    public string ProfileName
    {
        get
        {
            if (isMultiplayer)
            {
                return playerIndex == 2
                    ? PlayerPrefs.GetString("Player2Name", "Player2")
                    : PlayerPrefs.GetString("Player1Name", "Player1");
            }
            return PlayerPrefs.GetString("CurrentPlayerName", "Player");
        }
    }

    private bool isMultiplayer = false;
    private Player allyPlayer;
    private int lastDisplayedCharges = -1;
    private static float lastGlobalTransferTime = 0f;
    private const float TRANSFER_COOLDOWN = 0.35f;

    private void Awake()
    {   
        _movement = GetComponent<Movement>();

        if (playerIndex == 0)
        {
            playerIndex = gameObject.name.Contains("2") ? 2 : 1;
        }

        SetupSharingCollider();
    }

    void Start()
    {
        isMultiplayer = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SampleSceneMultiplayer";

        if (isMultiplayer)
        {
            SetupSharingCollider();
            FindAllyPlayer();

            if (expText == null && chargesText == null)
            {
                AutoFindExpText();
            }
            else if (expText == null && chargesText != null)
            {
                expText = chargesText;
            }
            else if (chargesText == null && expText != null)
            {
                chargesText = expText;
            }

            UpdateChargesText();
            Debug.Log($"<color=cyan>[Player] Initialized Player {playerIndex} ('{name}') -> Profile: '{ProfileName}', Spray Charges: {RemainingCharges}</color>");
        }
    }

    void Update()
    {
        // Обновляем текст оставшихся зарядов пшикалки, если значение изменилось
        if (isMultiplayer && (expText != null || chargesText != null))
        {
            int charges = RemainingCharges;
            if (charges != lastDisplayedCharges)
            {
                UpdateChargesText();
            }
        }

        if (!isMultiplayer) return;

        if (allyPlayer == null)
        {
            FindAllyPlayer();
        }

        // Близкий контакт двух персонажей: если подошли вплотную (дистанция <= 2.2), запускаем выравнивание
        if (allyPlayer != null)
        {
            float dist = Vector2.Distance(transform.position, allyPlayer.transform.position);
            if (dist <= 2.2f)
            {
                ShareBetween(this, allyPlayer);
            }
        }
    }

    private void FindAllyPlayer()
    {
        Player[] allPlayers = Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (Player p in allPlayers)
        {
            if (p != this)
            {
                allyPlayer = p;
                break;
            }
        }
    }

    /// <summary>
    /// Настраивает маленький триггер-коллайдер PlayerSharingZone для обмена зарядами при близком контакте.
    /// </summary>
    public void SetupSharingCollider()
    {
        Transform existingZone = transform.Find("PlayerSharingZone");
        GameObject zoneGO;
        if (existingZone != null)
        {
            zoneGO = existingZone.gameObject;
        }
        else
        {
            zoneGO = new GameObject("PlayerSharingZone");
            zoneGO.transform.SetParent(transform, false);
            zoneGO.transform.localPosition = Vector3.zero;
        }

        CircleCollider2D col = zoneGO.GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = zoneGO.AddComponent<CircleCollider2D>();
        }
        col.isTrigger = true;
        col.radius = sharingColliderRadius;

        PlayerSharingZone zone = zoneGO.GetComponent<PlayerSharingZone>();
        if (zone == null)
        {
            zone = zoneGO.AddComponent<PlayerSharingZone>();
        }
        zone.Init(this, sharingColliderRadius);
    }

    public void AddCarriedExp(int amount)
    {
        if (amount <= 0) return;
        EXP += amount;
    }

    /// <summary>
    /// Заполняет собственные заряды пшикалки из подобранного EXP инвентаря, если пшикалка не полная.
    /// </summary>
    public void ConvertExpToCharges()
    {
        if (EXP <= 0 || MagnetComponent == null) return;
        int missing = MagnetComponent.GetMissingCharges();
        if (missing > 0)
        {
            int toConvert = Mathf.Min(EXP, missing);
            EXP -= toConvert;
            MagnetComponent.AddCharges(toConvert);
            UpdateChargesText();
        }
    }

    /// <summary>
    /// Главная логика балансировки между двумя игроками:
    /// Тот у кого больше ресурсов (заряды + EXP) передает тому у кого меньше нужный объем,
    /// чтобы у обоих осталось +- поровну зарядов.
    /// </summary>
    public static void ShareBetween(Player p1, Player p2)
    {
        if (p1 == null || p2 == null || p1 == p2) return;
        if (!p1.isMultiplayer && !p2.isMultiplayer) return;

        if (Time.time - lastGlobalTransferTime < TRANSFER_COOLDOWN)
        {
            return;
        }

        // Кто богаче, тот донор, кто беднее — получатель
        Player donor = (p1.TotalResources >= p2.TotalResources) ? p1 : p2;
        Player receiver = (donor == p1) ? p2 : p1;

        int donorTotal = donor.TotalResources;
        int receiverTotal = receiver.TotalResources;
        int diff = donorTotal - receiverTotal;

        // Если у донора есть подобранный EXP или разница зарядов > 1
        if (diff > 1 || (diff == 1 && donor.EXP > 0))
        {
            int amountToTransfer = Mathf.Max(1, diff / 2);

            // Донор отдает: сначала из свободного инвентаря EXP, а если не хватает — из зарядов пшикалки
            int expGiven = Mathf.Min(donor.EXP, amountToTransfer);
            int chargesGiven = amountToTransfer - expGiven;

            donor.EXP -= expGiven;
            if (chargesGiven > 0 && donor.MagnetComponent != null)
            {
                donor.MagnetComponent.currentCharges = Mathf.Max(0, donor.MagnetComponent.currentCharges - chargesGiven);
                donor.MagnetComponent.UpdateChargesUI();
            }

            // Получатель принимает: пополняет заряды пшикалки (до maxCharges = 5), а излишек в свободный EXP
            int receiverMissing = receiver.MagnetComponent != null ? receiver.MagnetComponent.GetMissingCharges() : 0;
            int chargesReceived = Mathf.Min(amountToTransfer, receiverMissing);
            int expReceived = amountToTransfer - chargesReceived;

            if (receiver.MagnetComponent != null && chargesReceived > 0)
            {
                receiver.MagnetComponent.AddCharges(chargesReceived);
            }
            if (expReceived > 0)
            {
                receiver.AddCarriedExp(expReceived);
            }

            donor.ExpGiven += amountToTransfer;
            lastGlobalTransferTime = Time.time;

            if (donor.transferSound != null)
            {
                AudioSource.PlayClipAtPoint(donor.transferSound, donor.transform.position, 1f);
            }

            Debug.Log($"<color=green>[Multiplayer Sharing] {donor.name} ({donor.ProfileName}) передал {amountToTransfer} ед. игроку {receiver.name} ({receiver.ProfileName})! Итог: у {donor.name} {donor.RemainingCharges} зар. (+{donor.EXP} exp), у {receiver.name} {receiver.RemainingCharges} зар. (+{receiver.EXP} exp)</color>");
        }

        // Если после передачи остался свободный EXP, а своя пшикалка не полная — заполняем пшикалку!
        donor.ConvertExpToCharges();
        receiver.ConvertExpToCharges();

        donor.UpdateChargesText();
        receiver.UpdateChargesText();
    }

    public void UpdateChargesText()
    {
        int charges = RemainingCharges;
        lastDisplayedCharges = charges;
        string textValue = charges.ToString();
        if (expText != null)
        {
            expText.text = textValue;
        }
        if (chargesText != null && chargesText != expText)
        {
            chargesText.text = textValue;
        }
    }

    // Сохраняем вызов UpdateExpText для обратной совместимости
    public void UpdateExpText()
    {
        UpdateChargesText();
    }

    private void AutoFindExpText()
    {
        if (expText != null || chargesText != null) return;

        TextMeshProUGUI[] allTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allTexts == null || allTexts.Length == 0) return;

        string targetIndexStr = playerIndex.ToString();

        // Исключаем системные тексты UI игры
        var relevantTexts = allTexts.Where(t => {
            string n = t.gameObject.name.ToLower();
            return !n.Contains("time") && !n.Contains("enemy") && !n.Contains("enemies") &&
                   !n.Contains("viewstats") && !n.Contains("border") && !n.Contains("fps") &&
                   !n.Contains("pause") && !n.Contains("defeat") && !n.Contains("victory");
        }).ToList();

        // 1. По ключевым словам charge/exp/spray/ammo и номеру игрока
        foreach (var t in relevantTexts)
        {
            string n = t.gameObject.name.ToLower();
            bool matchesPlayer = n.Contains(targetIndexStr) || (playerIndex == 1 && n.Contains("p1")) || (playerIndex == 2 && n.Contains("p2"));
            bool matchesKeyword = n.Contains("charge") || n.Contains("exp") || n.Contains("spray") || n.Contains("ammo");
            if (matchesKeyword && matchesPlayer)
            {
                expText = t;
                chargesText = t;
                return;
            }
        }

        // 2. По имени игрока (Player1, P1, Player2, P2)
        foreach (var t in relevantTexts)
        {
            string n = t.gameObject.name.ToLower();
            bool matchesPlayer = (playerIndex == 1 && (n.Contains("player1") || n.Contains("p1") || n.EndsWith("1"))) ||
                                 (playerIndex == 2 && (n.Contains("player2") || n.Contains("p2") || n.EndsWith("2")));
            if (matchesPlayer)
            {
                expText = t;
                chargesText = t;
                return;
            }
        }

        // 3. Кандидаты по ключевым словам
        var keywordCandidates = relevantTexts.Where(t => {
            string n = t.gameObject.name.ToLower();
            return n.Contains("charge") || n.Contains("spray") || n.Contains("ammo") || n.Contains("exp");
        }).ToList();

        if (keywordCandidates.Count >= 2)
        {
            keywordCandidates.Sort((a, b) => string.Compare(a.gameObject.name, b.gameObject.name, System.StringComparison.Ordinal));
            expText = playerIndex == 2 ? keywordCandidates[1] : keywordCandidates[0];
            chargesText = expText;
            return;
        }

        // 4. Если осталось ровно 2 релевантных UI текста
        if (relevantTexts.Count == 2)
        {
            relevantTexts.Sort((a, b) => string.Compare(a.gameObject.name, b.gameObject.name, System.StringComparison.Ordinal));
            expText = playerIndex == 2 ? relevantTexts[1] : relevantTexts[0];
            chargesText = expText;
            return;
        }
    }

    private void PickUpExp(GameObject expObj)
    {
        if (expObj == null) return;
        EXP++;
        ExpCollected++;
        Debug.Log($"<color=yellow>[Multiplayer EXP] {name} ({ProfileName}) подобрал EXP! В инвентаре: {EXP} (всего собрано: {ExpCollected})</color>");
        Destroy(expObj);

        // Если союзник уже вплотную, сразу производим обмен!
        if (isMultiplayer && allyPlayer != null)
        {
            float dist = Vector2.Distance(transform.position, allyPlayer.transform.position);
            if (dist <= 2.2f)
            {
                ShareBetween(this, allyPlayer);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("EXP"))
        {
            // Подбираем EXP, только когда он долетел вплотную к телу
            BoxCollider2D body = GetComponent<BoxCollider2D>();
            float dist = Vector2.Distance(transform.position, collision.transform.position);
            if (dist <= 1.4f || (body != null && collision.IsTouching(body)))
            {
                PickUpExp(collision.gameObject);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("EXP"))
        {
            BoxCollider2D body = GetComponent<BoxCollider2D>();
            float dist = Vector2.Distance(transform.position, collision.transform.position);
            if (dist <= 1.4f || (body != null && collision.IsTouching(body)))
            {
                PickUpExp(collision.gameObject);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleBodyCollision(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleBodyCollision(collision.collider);
    }

    private void HandleBodyCollision(Collider2D otherCol)
    {
        if (!isMultiplayer || otherCol == null) return;
        Player other = otherCol.GetComponentInParent<Player>();
        if (other != null && other != this)
        {
            ShareBetween(this, other);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, sharingColliderRadius * (transform.lossyScale.x > 0 ? transform.lossyScale.x : 0.5f));
    }
}
