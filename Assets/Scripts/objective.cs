using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class objective : MonoBehaviour
{
    [Header("UI & Screens")]
    public GameObject pausePanel;
    public GameObject losePanel;
    public GameObject winPanel;
    public TextMeshProUGUI pauseTimeText;
    public TextMeshProUGUI loseTimeText;
    public TextMeshProUGUI winTimeText;
    public string mainMenuSceneName = "MainMenu";

    [Header("Game Settings")]
    private float gameTimer = 0f;
    private bool isPaused = false;
    private bool isGameWon = false;

    public GameObject background;
    public GameObject lightEnemy;
    private float distance;
    private Vector3 spawnPos;
    private float bound_x;
    private float x;
    private float bound_y;
    private float y;
    float safeDistance = 20f;
    private int enemyIndex;
    public float EnemiesLeft;

    public GameObject blueEnemy;
    public GameObject redEnemy;
    public GameObject yellowEnemy;

    private Dictionary<int, string> state = new Dictionary<int, string>()
    {
        [1] = "BudYellow",
        [2] = "BudGreen",
        [3] = "FlowerNoCharge",
        [4] = "FlowerWCharge"
    };

    private int[] branches = new int[5] { 1, 2, 2, 2, 2 };
    private Dictionary<string, GameObject> visualCache = new Dictionary<string, GameObject>();
    private bool wasChanged = false;
    string targetKey;
    private bool canRestart = false;
    public float multiplier = 1.0f;
    private PowerUpManagement UI;
    private int enemiesToSpawn;
    public int enemiesRemainingInWave;
    private int currentWave;

    public Dictionary<string, int> missedStats = new Dictionary<string, int>
    {
        { "TOP", 0 },
        { "BOTTOM", 0 },
        { "LEFT", 0 },
        { "RIGHT", 0 },
        { "TOTAL", 0 }
    };
    Dictionary<string, List<int>> fourDirections = new Dictionary<string, List<int>>()
    {
        { "TOP",    new List<int> { 45, 135 } },
        { "BOTTOM", new List<int> { 225, 315 } },
        { "LEFT",   new List<int> { 135, 225 } },
        { "RIGHT",  new List<int> { 315, 45 } }
    };
    public int[] missedColors = { 0, 0, 0, 0 };

    private bool isDataSaved = false;

    private void Awake()
    {
        UI = GameObject.FindFirstObjectByType<PowerUpManagement>(FindObjectsInactive.Include);

        foreach (Transform child in transform)
        {
            visualCache.Add(child.name, child.gameObject);
        }

        ApplyVisualsFromBushes();

        if (pausePanel != null) pausePanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
    }

    void Start()
    {
        Time.timeScale = 1f;
        gameTimer = 0f;

        bound_x = background.GetComponent<SpriteRenderer>().bounds.size.x;
        bound_y = background.GetComponent<SpriteRenderer>().bounds.size.y;
        StartCoroutine(waveManager());
    }

    void Update()
    {
        if (!isPaused && !canRestart && !isGameWon)
        {
            gameTimer += Time.deltaTime;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame && !canRestart && !isGameWon)
        {
            TogglePause();
        }

        if (Keyboard.current.rKey.wasPressedThisFrame && canRestart)
        {
            RestartGame();
        }

        if (wasChanged)
        {
            // 1. Находим первую доступную для прокачки ветку (которая жива, но еще не 4 уровня)
            int upgradeTarget = -1;
            for (int i = 1; i <= 4; i++)
            {
                if (branches[i] >= 2 && branches[i] < 4)
                {
                    upgradeTarget = i;
                    break;
                }
            }

            if (upgradeTarget != -1)
            {
                branches[upgradeTarget]++;
            }

            // 2. Логика "исцеления" убитой ветки за счет полностью выращенной
            if (branches.Skip(1).Any(i => i == 4) && branches.Skip(1).Any(i => i == 1))
            {
                int maxIndex = System.Array.FindIndex(branches, 1, i => i == 4);
                int minIndex = System.Array.FindIndex(branches, 1, i => i == 1);

                branches[maxIndex]--;
                branches[minIndex]++;
            }

            ApplyVisualsFromBushes();
            wasChanged = false;

            // 3. ПЕРЕВІРКА НА ПЕРЕМОГУ: Якщо всі 4 гілки виросли до 4 рівня
            if (branches[1] == 4 && branches[2] == 4 && branches[3] == 4 && branches[4] == 4)
            {
                TriggerVictory();
            }
        }

        if (canRestart && !isDataSaved && !isGameWon)
        {
            TriggerDefeat();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;

        if (isPaused && pauseTimeText != null)
        {
            pauseTimeText.text = "Час: " + FormatTime(gameTimer);
        }
    }

    private void TriggerDefeat()
    {
        isDataSaved = true;
        Time.timeScale = 0f;

        if (losePanel != null) losePanel.SetActive(true);
        if (loseTimeText != null) loseTimeText.text = "Час: " + FormatTime(gameTimer);

        SaveBestTimeIfNeeded();

        missedStats["TOTAL"] = missedStats["RIGHT"] + missedStats["LEFT"] + missedStats["TOP"] + missedStats["BOTTOM"];
        DataToCSV csvManager = UnityEngine.Object.FindFirstObjectByType<DataToCSV>();
        if (csvManager == null)
        {
            csvManager = gameObject.AddComponent<DataToCSV>();
        }
        csvManager.LogDefeatData(missedStats, missedColors);
    }

    private void TriggerVictory()
    {
        isGameWon = true;
        Time.timeScale = 0f;

        if (winPanel != null) winPanel.SetActive(true);
        if (winTimeText != null) winTimeText.text = "Час: " + FormatTime(gameTimer);

        SaveBestTimeIfNeeded();
    }

    private void SaveBestTimeIfNeeded()
    {
        string playerName = PlayerPrefs.GetString("CurrentPlayerName", "Unknown");
        string csvPath = System.IO.Path.Combine(Application.persistentDataPath, "Accounts.csv");

        if (!System.IO.File.Exists(csvPath)) return;

        string[] lines = System.IO.File.ReadAllLines(csvPath);
        bool fileUpdated = false;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] data = lines[i].Split(',');
            if (data[0] == playerName)
            {
                float oldBestTime = 0f;
                if (data.Length > 2)
                {
                    float.TryParse(data[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out oldBestTime);
                }

                if (oldBestTime == 0f || gameTimer > oldBestTime)
                {
                    string creationDate = data.Length > 1 ? data[1] : "";
                    string newTimeStr = gameTimer.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    lines[i] = $"{playerName},{creationDate},{newTimeStr}";
                    fileUpdated = true;
                    Debug.Log($"<color=green>[CSV] Новый рекорд для {playerName}: {newTimeStr}</color>");
                }
                break;
            }
        }

        if (fileUpdated)
        {
            System.IO.File.WriteAllLines(csvPath, lines);
        }
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private string FormatTime(float timeInSeconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(timeInSeconds);
        return time.ToString(@"hh\:mm\:ss");
    }

    private void DamageSystem(GameObject enemy)
    {
        if (canRestart || isGameWon) return;

        if (enemy.name.Contains("EnemyGeneric")) missedColors[0]++;
        else if (enemy.name.Contains("BlueEnemy1")) missedColors[1]++;
        else if (enemy.name.Contains("RedEnemy 1")) missedColors[2]++;
        else if (enemy.name.Contains("YellowEnemy1")) missedColors[3]++;

        // 1. Находим первую ЖИВУЮ ветку и наносим ей урон
        int damageTarget = -1;
        for (int i = 1; i <= 4; i++)
        {
            if (branches[i] > 1)
            {
                damageTarget = i;
                break;
            }
        }

        if (damageTarget != -1)
        {
            branches[damageTarget]--;
        }

        // 2. Проверяем, не уничтожены ли все 4 ветки (поражение)
        bool allDead = true;
        for (int i = 1; i <= 4; i++)
        {
            if (branches[i] > 1)
            {
                allDead = false;
                break;
            }
        }

        if (allDead && !isGameWon)
        {
            canRestart = true;
        }

        ApplyVisualsFromBushes();
    }

    private void ApplyVisualsFromBushes()
    {
        foreach (var pair in visualCache)
        {
            pair.Value.SetActive(false);
        }

        for (int i = 1; i <= 4; i++)
        {
            int currentStatus = branches[i];

            if (currentStatus == 4)
            {
                if (visualCache.ContainsKey(i + "BudGreen")) visualCache[i + "BudGreen"].SetActive(true);
                if (visualCache.ContainsKey(i + "FlowerWCharge")) visualCache[i + "FlowerWCharge"].SetActive(true);
                if (visualCache.ContainsKey(i + "Shine")) visualCache[i + "Shine"].SetActive(true);
            }
            else if (currentStatus == 3)
            {
                if (visualCache.ContainsKey(i + "BudGreen")) visualCache[i + "BudGreen"].SetActive(true);
                if (visualCache.ContainsKey(i + "FlowerNoCharge")) visualCache[i + "FlowerNoCharge"].SetActive(true);
            }
            else
            {
                string key = i + state[currentStatus];
                if (visualCache.ContainsKey(key)) visualCache[key].SetActive(true);
            }
        }
    }

    IEnumerator Spawner()
    {
        GameObject[] enemies = { lightEnemy, blueEnemy, redEnemy, yellowEnemy };
        int spawnedCount = 0;
        int targetCount = enemiesToSpawn;
        int step = Mathf.Max(1, targetCount / enemies.Length);

        while (spawnedCount < targetCount)
        {
            // Ждем и не выполняем тело цикла, пока включена пауза
            yield return new WaitWhile(() => isPaused);

            var topTwoValues = missedStats.Where(x => x.Key != "TOTAL").OrderByDescending(x => x.Value).Select(x => x.Value).Take(2).ToList();
            var topTwoKeys = missedStats.Where(x => x.Key != "TOTAL").OrderByDescending(x => x.Value).Select(x => x.Key).Take(2).ToList();
            if (topTwoValues[0] - topTwoValues[1] >= 2 && currentWave >= 2)
            {
                Vector3 baseDirection = Vector3.right;
                List<int> limits = fourDirections[topTwoKeys[0]];
                int angle = 0;
                if (limits[0] > limits[1])
                {
                    angle = UnityEngine.Random.Range(limits[0], limits[1] + 360) % 360;
                }
                else
                {
                    angle = UnityEngine.Random.Range(limits[0], limits[1]);
                }
                Quaternion rotation = Quaternion.Euler(0, 0, angle);
                Vector3 rotatedDirection = rotation * baseDirection;
                float maxDistance = Mathf.Min(bound_x, bound_y) / 2f;
                float randomLength = UnityEngine.Random.Range(safeDistance, maxDistance);
                Vector3 spawnOffset = rotatedDirection * randomLength;
                spawnPos = transform.position + spawnOffset;
            }
            else
            {
                x = UnityEngine.Random.Range(-bound_x / 2, bound_x / 2);
                y = UnityEngine.Random.Range(-bound_y / 2, bound_y / 2);
                spawnPos = new Vector3(x, y, 0);
            }

            if (Vector3.Distance(spawnPos, transform.position) > safeDistance)
            {
                enemyIndex = (spawnedCount / step) % enemies.Length;
                Instantiate(enemies[enemyIndex], spawnPos, Quaternion.identity);
                spawnedCount++;

                // ИСПОЛЬЗУЕМ WaitForSeconds (он реагирует на Time.timeScale = 0)
                yield return new WaitForSeconds(2.5f);
            }
            else
            {
                yield return null;
            }
        }
        currentWave++;
    }

    IEnumerator waveManager()
    {
        while (true)
        {
            enemiesToSpawn = (int)(8 * multiplier);
            enemiesRemainingInWave = enemiesToSpawn;

            // Ждем завершения спавна
            yield return StartCoroutine(Spawner());

            // Ждем, пока игрок не добьет всех врагов волны
            yield return new WaitUntil(() => enemiesRemainingInWave <= 0);

            // --- НОВОЕ УСЛОВИЕ ПОБЕДЫ ---
            // В корутине Spawner() переменная currentWave увеличивается в конце спавна.
            // Значит, когда мы добили врагов и currentWave равна 5 - это была пятая волна.
            if (currentWave >= 5 && !isGameWon && !canRestart)
            {
                TriggerVictory();
                yield break; // Останавливаем корутину, чтобы не открывалось меню прокачки
            }
            // ----------------------------

            if (UI != null)
            {
                UI.OpenUpgradeMenu();
                yield return new WaitUntil(() => !UI.isMenuActive);
            }

            multiplier += 0.5f;
            wasChanged = true;
        }
    }

    public void OpenAnalytics()
    {
        try
        {
            string targetPath = System.IO.Path.Combine(Application.persistentDataPath, "analytics.exe");

            if (System.IO.File.Exists(targetPath))
            {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.FileName = targetPath;
                startInfo.WorkingDirectory = Application.persistentDataPath;

                System.Diagnostics.Process.Start(startInfo);
                UnityEngine.Debug.Log("<color=green>Успешный запуск аналитики из AppData: </color>" + targetPath);
            }
            else
            {
                UnityEngine.Debug.LogError("<color=red>Файл не найден в AppData!</color> Искал здесь: " + targetPath);
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Ошибка запуска аналитики: " + e.Message);
        }
    }

    public void CountEnemyDeath()
    {
        enemiesRemainingInWave--;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (canRestart || isGameWon) return;

        if (collision != null && collision.CompareTag("Enemy"))
        {
            Vector3 relativeDirection = collision.transform.position - transform.position;
            float angle = Mathf.Atan2(relativeDirection.y, relativeDirection.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            CheckImpactSide(angle);
            DamageSystem(collision.gameObject);
            CountEnemyDeath();
            Destroy(collision.gameObject);
        }
    }
    
    private void CheckImpactSide(float impactAngle)
    {
        if (impactAngle >= 45f && impactAngle < 135f) { missedStats["TOP"]++; }
        else if (impactAngle >= 135f && impactAngle < 225f) { missedStats["LEFT"]++; }
        else if (impactAngle >= 225f && impactAngle < 315f) { missedStats["BOTTOM"]++; }
        else { missedStats["RIGHT"]++; }
    }
}