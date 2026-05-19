using System;
using System.Collections;
using UnityEngine;

public class LawnMower : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject mowerPrefab;
    [SerializeField] private GameObject basePrefab;

    private Transform player;
    Vector3 pos1;
    Vector3 pos2;
    private GameObject spawnedMower;
    private GameObject spawnedBase1;
    private GameObject spawnedBase2;

    private float trackExtension = -0.8f;
    private bool isMowerActive = false;
    private float mowerSpeed = 10f;

    // --- NEW VARIABLES FOR UI ---
    [HideInInspector] public float countdownTimer = 0f;
    [HideInInspector] public bool isPlacing = false;

    void Awake() { player = GameObject.FindWithTag("Player").transform; }

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

        // Phase 1: Wait to place the first base
        countdownTimer = 5f; // 1.5 seconds (Matches your original 15 * 0.1f)
        while (countdownTimer > 0)
        {
            countdownTimer -= Time.deltaTime;
            yield return null;
        }

        spawnedBase1 = Instantiate(basePrefab, player.position, Quaternion.identity);
        spawnedMower = Instantiate(mowerPrefab, player.position, Quaternion.identity);

        // Phase 2: Wait to place the second base
        countdownTimer = 5f;
        while (countdownTimer > 0)
        {
            countdownTimer -= Time.deltaTime;
            yield return null;
        }

        spawnedBase2 = Instantiate(basePrefab, player.position, Quaternion.identity);
        Vector3 direction = spawnedBase2.transform.position - spawnedMower.transform.position;
        float angle = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90f;
        Quaternion Rotation = Quaternion.Euler(0, 0, angle);

        spawnedBase2.transform.rotation = Rotation * Quaternion.Euler(0, 0, 180);
        spawnedBase1.transform.rotation = Rotation;
        spawnedMower.transform.rotation = Rotation * Quaternion.Euler(0, 0, 180);

        isPlacing = false;
        isMowerActive = true;
    }

    void Update()
    {
        if (isMowerActive && spawnedBase1 != null && spawnedBase2 != null)
        {
            pos1 = spawnedBase1.transform.position;
            pos2 = spawnedBase2.transform.position;
            Vector3 direction = (pos2 - pos1).normalized;
            Vector3 Start = pos1 - (direction * trackExtension);
            Vector3 End = pos2 + (direction * trackExtension);
            float distance = Vector3.Distance(Start, End);
            float velocity = mowerSpeed / distance;
            float t = Mathf.PingPong(Time.time * velocity, 1f);
            spawnedMower.transform.position = Vector3.Lerp(Start, End, t);
        }
    }
}