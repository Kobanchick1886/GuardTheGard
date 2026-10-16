using UnityEngine;

public class Player : MonoBehaviour
{
    private Movement _movement;
    [SerializeField] private int EXP = 0;
    
    [Header("Multiplayer Settings")]
    [SerializeField] private float transferDistance = 2.5f;
    [SerializeField] private AudioClip transferSound;

    private bool isMultiplayer = false;
    private Player allyPlayer;

    public int CarriedExp => EXP;

    private void Awake()
    {   
        _movement = GetComponent<Movement>();
    }

    void Start()
    {
        isMultiplayer = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SampleSceneMultiplayer";
    }

    void Update()
    {
        if (!isMultiplayer || EXP <= 0) return;

        // Поиск союзника в мультиплеере (второго игрока)
        if (allyPlayer == null)
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

        if (allyPlayer != null)
        {
            float dist = Vector2.Distance(transform.position, allyPlayer.transform.position);
            if (dist <= transferDistance)
            {
                // Пополняем пшикалку СОЮЗНИКУ (и только союзнику!)
                Magnet allyMagnet = allyPlayer.GetComponentInChildren<Magnet>();
                if (allyMagnet != null && allyMagnet.CanAddCharges())
                {
                    int missing = allyMagnet.GetMissingCharges();
                    int toTransfer = Mathf.Min(EXP, missing);
                    if (toTransfer > 0)
                    {
                        EXP -= toTransfer;
                        allyMagnet.AddCharges(toTransfer);

                        if (transferSound != null)
                        {
                            AudioSource.PlayClipAtPoint(transferSound, allyPlayer.transform.position, 1f);
                        }
                        Debug.Log($"<color=green>[Multiplayer] {name} передал {toTransfer} EXP союзнику {allyPlayer.name}! Зарядов у союзника: {allyMagnet.currentCharges}/{allyMagnet.maxCharges}</color>");
                    }
                }
            }
        }
    }

    private void OnGUI()
    {
        if (!isMultiplayer) return;

        // Отображение количества собранного EXP над головой персонажа
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.6f);
        if (screenPos.z > 0)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 15;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            if (EXP > 0)
            {
                style.normal.textColor = Color.yellow;
                GUI.Label(new Rect(screenPos.x - 70, Screen.height - screenPos.y - 12, 140, 24), $"{name}: {EXP} EXP", style);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("EXP"))
        {
            if (collision.IsTouching(GetComponent<BoxCollider2D>()))
            {
                EXP++;
                Destroy(collision.gameObject);
            }
        }
    }
}
