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
    public static MultiplayerExpManager Instance { get; private set; }

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitializeForMultiplayer()
    {
        if (SceneManager.GetActiveScene().name == "SampleSceneMultiplayer")
        {
            if (FindFirstObjectByType<MultiplayerExpManager>() == null)
            {
                GameObject managerGO = new GameObject("MultiplayerExpManager");
                managerGO.AddComponent<MultiplayerExpManager>();
                Debug.Log("<color=green>[MultiplayerExpManager] Auto-initialized for SampleSceneMultiplayer.</color>");
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        isMultiplayerScene = SceneManager.GetActiveScene().name == "SampleSceneMultiplayer";
    }

    private void Start()
    {
        if (!isMultiplayerScene) return;

        LoadExpPrefabIfNeeded();
        CalculateMapBounds();
        ScatterInitialExp();
        StartCoroutine(PeriodicExpReplenishment());
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void LoadExpPrefabIfNeeded()
    {
        if (expPrefab == null)
        {
            expPrefab = Resources.Load<GameObject>("EXP");
        }
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
        if (expPrefab == null)
        {
            Debug.LogWarning("[MultiplayerExpManager] EXP prefab not found in Resources or Inspector!");
            return;
        }

        for (int i = 0; i < initialExpCount; i++)
        {
            SpawnExpAtRandomPosition();
        }

        Debug.Log($"<color=yellow>[MultiplayerExpManager] Scattered {initialExpCount} EXP orbs across the map.</color>");
    }

    private IEnumerator PeriodicExpReplenishment()
    {
        WaitForSeconds wait = new WaitForSeconds(respawnCheckInterval);

        while (isMultiplayerScene)
        {
            yield return wait;

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
            Instantiate(expPrefab, enemyPosition, Quaternion.identity);
        }
    }

    private void SpawnExpAtRandomPosition()
    {
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
