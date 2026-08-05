using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro; // Потрібно для роботи з текстом
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
    public string mainMenuSceneName = "MainMenu"; // Назва сцени головного меню

    [Header("Game Settings")]
    public int wavesToWin = 5; // Кількість хвиль для появи екрану Перемоги
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
    private int branch = 1;
    private Dictionary<string, GameObject> visualCache = new Dictionary<string, GameObject>();
    private bool wasChanged = false;
    string targetKey;
    private bool canRestart = false; // Використовується як прапорець поразки
    private float multiplier = 1.0f;
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
        {   { "TOP",    new List<int> { 45, 135 } },
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

        // Ховаємо всі панелі при старті
        if (pausePanel != null) pausePanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
    }

    void Start()
    {
        Time.timeScale = 1f; // Впевнюємось, що час йде
        gameTimer = 0f;

        bound_x = background.GetComponent<SpriteRenderer>().bounds.size.x;
        bound_y = background.GetComponent<SpriteRenderer>().bounds.size.y;
        StartCoroutine(waveManager());
    }

    // Змінено з FixedUpdate на Update для кращої обробки інпутів та таймера
    void Update()
    {
        // 1. Оновлення таймера (якщо гра не на паузі і не закінчена)
        if (!isPaused && !canRestart && !isGameWon)
        {
            gameTimer += Time.deltaTime;
        }

        // 2. Обробка паузи по ESC
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !canRestart && !isGameWon)
        {
            TogglePause();
        }

        // 3. Обробка рестарту при поразці
        if (Keyboard.current.rKey.wasPressedThisFrame && canRestart)
        {
            RestartGame();
        }

        if (wasChanged)
        {
            switch (branches[branch])
            {
                case 4:
                    branch++;
                    break;
                case 3:
                    branches[branch]++;
                    branch++;
                    break;
                case 2:
                    branches[branch]++;
                    break;
            }

            if (branches.Any(i => i == 4) && branches.Skip(1).Any(i => i == 1))
            {
                int index = System.Array.FindIndex(branches, i => i == 4);
                branches[index]--;

                index = System.Array.FindIndex(branches, 1, i => i == 1);
                branches[index]++;
            }

            ApplyVisualsFromBushes();
            wasChanged = false;
        }

        // Логіка поразки
        if (canRestart && !isDataSaved)
        {
            TriggerDefeat();
        }
    }

    // --- ЛОГІКА UI ТА СТАНІВ ГРИ ---

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
        Time.timeScale = 0f; // Зупиняємо гру

        if (losePanel != null) losePanel.SetActive(true);
        if (loseTimeText != null) loseTimeText.text = "Час: " + FormatTime(gameTimer);

        // Збереження даних
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
        Time.timeScale = 0f; // Зупиняємо гру

        if (winPanel != null) winPanel.SetActive(true);
        if (winTimeText != null) winTimeText.text = "Час: " + FormatTime(gameTimer);

        // Тут також можна додати збереження даних для перемоги, якщо потрібно
    }

    // Метод для кнопки "Повернутися до головного меню"
    public void ReturnToMenu()
    {
        Time.timeScale = 1f; // Обов'язково повертаємо час перед завантаженням нової сцени
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Метод для кнопок "Грати знов!"
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private string FormatTime(float timeInSeconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(timeInSeconds);
        // Формат як на референсі: 02:32:01 (Години:Хвилини:Секунди)
        return time.ToString(@"hh\:mm\:ss");
    }

    // ---------------------------------

    private void DamageSystem(GameObject enemy)
    {
        if (canRestart || isGameWon) return;
        if (branch >= branches.Length) return;

        if (enemy.name.Contains("EnemyGeneric")) missedColors[0]++;
        else if (enemy.name.Contains("BlueEnemy1")) missedColors[1]++;
        else if (enemy.name.Contains("RedEnemy 1")) missedColors[2]++;
        else if (enemy.name.Contains("YellowEnemy1")) missedColors[3]++;

        branches[branch]--;

        if (branches[branch] <= 1)
        {
            branch++;
        }
        if (branch >= branches.Length)
        {
            canRestart = true; // Тригерить поразку в Update
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
                yield return new WaitForSecondsRealtime(2.5f);
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
            // Перевірка на перемогу
            if (currentWave >= wavesToWin)
            {
                TriggerVictory();
                yield break; // Зупиняємо хвилі
            }

            enemiesToSpawn = (int)(8 * multiplier);
            enemiesRemainingInWave = enemiesToSpawn;
            yield return StartCoroutine(Spawner());
            yield return new WaitUntil(() => enemiesRemainingInWave <= 0);

            if (UI != null && currentWave < wavesToWin)
            {
                UI.OpenUpgradeMenu();
                yield return new WaitUntil(() => !UI.isMenuActive);
            }
            multiplier += 0.5f;
            wasChanged = true;
        }
    }
    // Добавь это куда-нибудь в конец скрипта, перед последней закрывающей скобкой }
    // --- ОТКРЫТИЕ ВНЕШНЕЙ АНАЛИТИКИ (ИЗ APPDATA) ---
    public void OpenAnalytics()
    {
        try
        {
            string targetPath = System.IO.Path.Combine(Application.persistentDataPath, "analytics.exe");

            if (System.IO.File.Exists(targetPath))
            {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.FileName = targetPath;
                // ЖЕСТКО ЗАДАЕМ рабочую папку, чтобы экзешник искал csv-файлы именно там
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