using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages EXP orbs scattered across the map exclusively for SampleSceneMultiplayer.
/// Scatters initial EXP on scene start, drops EXP on enemy death, and periodically
/// replenishes map orbs so players can always recharge each other's sprays.
/// </summary>
public class MultiplayerExpManager : MonoBehaviour
{
    private static MultiplayerExpManager _instance;
    public static MultiplayerExpManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<MultiplayerExpManager>();
                if (_instance == null && SceneManager.GetActiveScene().name == "SampleSceneMultiplayer")
                {
                    EnsureManagerExists();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("EXP Prefab")]
    [SerializeField] private GameObject expPrefab;

    [Header("Map Scattering Settings")]
    [SerializeField] private int initialExpCount = 16;
    [SerializeField] private int maxExpOnMap = 18;
    [SerializeField] private float respawnCheckInterval = 8f;
    [SerializeField] private int respawnBatchSize = 3;

    private float boundX = 14f;
    private float boundY = 10f;
    private bool isMultiplayerScene = false;
    private Coroutine replenishmentCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoRegisterSceneListener()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCheckActiveScene()
    {
        if (SceneManager.GetActiveScene().name == "SampleSceneMultiplayer")
        {
            EnsureManagerExists();
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "SampleSceneMultiplayer")
        {
            EnsureManagerExists();
        }
    }

    public static MultiplayerExpManager EnsureManagerExists()
    {
        if (_instance == null)
        {
            _instance = FindFirstObjectByType<MultiplayerExpManager>();
        }

        if (_instance == null)
        {
            GameObject managerGO = new GameObject("MultiplayerExpManager");
            _instance = managerGO.AddComponent<MultiplayerExpManager>();
            Debug.Log("<color=green>[MultiplayerExpManager] Successfully auto-created for SampleSceneMultiplayer.</color>");
        }
        else
        {
            _instance.isMultiplayerScene = SceneManager.GetActiveScene().name == "SampleSceneMultiplayer";
            if (_instance.isMultiplayerScene && _instance.replenishmentCoroutine == null)
            {
                _instance.InitMultiplayer();
            }
        }

        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        isMultiplayerScene = SceneManager.GetActiveScene().name == "SampleSceneMultiplayer";
        LoadExpPrefabIfNeeded();
    }

    private void Start()
    {
        isMultiplayerScene = SceneManager.GetActiveScene().name == "SampleSceneMultiplayer";
        if (!isMultiplayerScene) return;

        InitMultiplayer();
    }

    public void InitMultiplayer()
    {
        isMultiplayerScene = true;
        LoadExpPrefabIfNeeded();
        CalculateMapBounds();
        ScatterInitialExp();

        if (replenishmentCoroutine != null)
        {
            StopCoroutine(replenishmentCoroutine);
        }
        replenishmentCoroutine = StartCoroutine(PeriodicExpReplenishment());
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public void LoadExpPrefabIfNeeded()
    {
        if (expPrefab == null)
        {
            expPrefab = Resources.Load<GameObject>("EXP");
        }

        if (expPrefab == null)
        {
            var existingInScene = FindFirstObjectByType<Exp>(FindObjectsInactive.Include);
            if (existingInScene != null)
            {
                expPrefab = existingInScene.gameObject;
            }
        }

#if UNITY_EDITOR
        if (expPrefab == null)
        {
            expPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/EXP.prefab");
            if (expPrefab == null)
            {
                expPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/EXP.prefab");
            }
        }
#endif
    }

    private void CalculateMapBounds()
    {
        GameObject bg = GameObject.Find("Background");
        if (bg != null && bg.TryGetComponent<SpriteRenderer>(out var sr))
        {
            boundX = (sr.bounds.size.x * 0.5f) - 2f;
            boundY = (sr.bounds.size.y * 0.5f) - 2f;
        }
        else
        {
            boundX = 14f;
            boundY = 9f;
        }
    }

    private void ScatterInitialExp()
    {
        LoadExpPrefabIfNeeded();
        if (expPrefab == null)
        {
            Debug.LogWarning("[MultiplayerExpManager] EXP prefab not found in Resources or Inspector!");
            return;
        }

        GameObject[] existing = GameObject.FindGameObjectsWithTag("EXP");
        int countToSpawn = Mathf.Max(0, initialExpCount - (existing != null ? existing.Length : 0));

        for (int i = 0; i < countToSpawn; i++)
        {
            SpawnExpAtRandomPosition();
        }

        Debug.Log($"<color=yellow>[MultiplayerExpManager] Scattered {countToSpawn} EXP orbs across the map.</color>");
    }

    private IEnumerator PeriodicExpReplenishment()
    {
        WaitForSeconds wait = new WaitForSeconds(respawnCheckInterval);

        while (isMultiplayerScene)
        {
            yield return wait;

            LoadExpPrefabIfNeeded();
            if (expPrefab == null) continue;

            // Считаем сколько сейчас EXP на карте
            GameObject[] activeOrbs = GameObject.FindGameObjectsWithTag("EXP");
            int currentCount = activeOrbs != null ? activeOrbs.Length : 0;

            if (currentCount < maxExpOnMap)
            {
                int toSpawn = Mathf.Min(respawnBatchSize, maxExpOnMap - currentCount);
                for (int i = 0; i < toSpawn; i++)
                {
                    SpawnExpAtRandomPosition();
                }
            }
        }
    }

    public void OnEnemyDied(Vector3 enemyPosition)
    {
        if (!isMultiplayerScene) return;

        LoadExpPrefabIfNeeded();
        if (expPrefab != null)
        {
            Vector3 spawnPos = new Vector3(enemyPosition.x, enemyPosition.y, 0f);
            Instantiate(expPrefab, spawnPos, Quaternion.identity);
            Debug.Log($"<color=yellow>[MultiplayerExpManager] Enemy dropped EXP at {spawnPos}</color>");
        }
        else
        {
            Debug.LogWarning("[MultiplayerExpManager] Cannot drop EXP: expPrefab is null!");
        }
    }

    private void SpawnExpAtRandomPosition()
    {
        LoadExpPrefabIfNeeded();
        if (expPrefab == null) return;

        Vector3 spawnPos = Vector3.zero;
        int attempts = 0;
        do
        {
            float rx = Random.Range(-boundX, boundX);
            float ry = Random.Range(-boundY, boundY);
            spawnPos = new Vector3(rx, ry, 0f);
            attempts++;
        }
        while (spawnPos.magnitude < 3f && attempts < 15); // не спавнить в упор к центральному цветку

        Instantiate(expPrefab, spawnPos, Quaternion.identity);
    }
}
