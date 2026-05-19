using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class objective : MonoBehaviour
{
    public GameObject background;
    public GameObject lightEnemy;
    private float distance;
    private Vector3 spawnPos;
    private float bound_x;
    private float x;
    private float bound_y;
    private float y;
    float safeDistance = 20f;
    public float EnemiesLeft;

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
    private bool canRestart = false;
    private float multiplier = 1.0f;
    private PowerUpManagement UI;
    private int enemiesToSpawn;
    public int enemiesRemainingInWave;

    public Dictionary<string, int> missedStats = new Dictionary<string, int>
    {
        { "TOP", 0 },
        { "BOTTOM", 0 },
        { "LEFT", 0 },
        { "RIGHT", 0 },
        { "TOTAL", 0 }
    };

    private bool isDataSaved = false;

    private void DamageSystem()
    {
        if (canRestart) return;
        if (branch >= branches.Length) return;

        targetKey = branch + state[branches[branch]];
        if (visualCache.ContainsKey(targetKey))
        {
            visualCache[targetKey].SetActive(false);
        }
        if (branches[branch] == 1)
        {
            branch++;
        }
        branches[branch]--;
        if (branches.All(i => i == 1) || branches.Skip(1).All(i => i == 4))
        {
            canRestart = true;

        }

    }

    private void Awake()
    {
        UI = GameObject.FindFirstObjectByType<PowerUpManagement>(FindObjectsInactive.Include);

        foreach (Transform child in transform)
        {
            visualCache.Add(child.name, child.gameObject);
            if (child.gameObject.name.Contains("WCharge") || child.gameObject.name.Contains("NoCharge"))
            {
                child.gameObject.SetActive(false);
            }
            else
            {
                child.gameObject.SetActive(true);
            }
        }
    }

    void Start()
    {
        bound_x = background.GetComponent<SpriteRenderer>().bounds.size.x;
        bound_y = background.GetComponent<SpriteRenderer>().bounds.size.y;
        StartCoroutine(waveManager());
    }

    void FixedUpdate()
    {
        if (wasChanged)
        {
            switch (branches[branch])
            {
                case 4:
                    branch++;
                    break;
                case 3:
                    branches[branch]++;
                    targetKey = branch + state[branches[branch]];
                    if (visualCache.ContainsKey(targetKey))
                    {
                        visualCache[targetKey].SetActive(true);
                    }
                    branch++;
                    break;
                case 2:
                    branches[branch]++;
                    targetKey = branch + state[branches[branch]];
                    if (visualCache.ContainsKey(targetKey))
                    {
                        visualCache[targetKey].SetActive(true);
                    }
                    break;
                
            }

            if (branches.Any(i => i == 4) && branches.Skip(1).Any(i => i == 1))
            {
                int index = System.Array.FindIndex(branches, i => i == 4);
                targetKey = index + state[branches[index]];
                if (visualCache.ContainsKey(targetKey))
                {
                    visualCache[targetKey].SetActive(false);
                }
                branches[index]--;
                index = System.Array.FindIndex(branches, 1, i => i == 1);
                branches[index]++;
                targetKey = index + state[branches[index]];
                if (visualCache.ContainsKey(targetKey))
                {
                    visualCache[targetKey].SetActive(true);
                }
            }
            wasChanged = false;
        }

        // DEFEAT LOGIC LOGGING
        if (canRestart && !isDataSaved)
        {
            isDataSaved = true;

            missedStats["TOTAL"] = missedStats["RIGHT"] + missedStats["LEFT"] + missedStats["TOP"] + missedStats["BOTTOM"];

            DataToCSV csvManager = Object.FindFirstObjectByType<DataToCSV>();
            if (csvManager == null)
            {
                csvManager = gameObject.AddComponent<DataToCSV>();
            }

            csvManager.LogDefeatData(missedStats);
        }

        if (Keyboard.current.rKey.wasPressedThisFrame && canRestart)
        {
            canRestart = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    IEnumerator Spawner()
    {
        int spawnedCount = 0;
        int targetCount = enemiesToSpawn;

        while (spawnedCount < targetCount)
        {
            x = UnityEngine.Random.Range(-bound_x / 2, bound_x / 2);
            y = UnityEngine.Random.Range(-bound_y / 2, bound_y / 2);
            spawnPos = new Vector3(x, y, 0);

            if (Vector3.Distance(spawnPos, transform.position) > safeDistance)
            {
                Instantiate(lightEnemy, spawnPos, Quaternion.identity);
                spawnedCount++;
                yield return new WaitForSecondsRealtime(2.5f);
            }
            else
            {
                yield return null;
            }
        }
    }

    IEnumerator waveManager()
    {
        while (true)
        {
            enemiesToSpawn = (int)(10 * multiplier);
            enemiesRemainingInWave = enemiesToSpawn;
            yield return StartCoroutine(Spawner());
            yield return new WaitUntil(() => enemiesRemainingInWave <= 0);
            if (UI != null)
            {
                UI.OpenUpgradeMenu();
                yield return new WaitUntil(() => !UI.isMenuActive);
            }
            multiplier += 0.5f;
            wasChanged = true;
        }
    }

    public void CountEnemyDeath()
    {
        enemiesRemainingInWave--;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // SHIELD: Ignore all enemy collisions if the game is already over
        if (canRestart) return;

        if (collision != null && collision.CompareTag("Enemy"))
        {
            Vector3 relativeDirection = collision.transform.position - transform.position;
            float angle = Mathf.Atan2(relativeDirection.y, relativeDirection.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            CheckImpactSide(angle);
            DamageSystem();
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