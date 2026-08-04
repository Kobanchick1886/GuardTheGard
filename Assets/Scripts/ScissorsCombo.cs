using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Обязательно добавляем для UI

public class ScissorsCombo : MonoBehaviour
{
    public GameObject scissors;
    private List<GameObject> enemiesInRange = new List<GameObject>();

    // canSnip теперь будет автоматически включаться после отработки кулдауна
    public bool canSnip = false;
    private bool isCountingDown = false;
    public int cuttedEnemies = 0;

    [Header("Cooldown Settings")]
    public float cooldownTime = 4f; // Настрой нужное время
    public Image cooldownFillImage; // Сюда перетащи SC_Bar Fill
    private float currentCooldown = 0f;

    private void Update()
    {
        // Если способность на кулдауне (уже использована, но мы ждем отката)
        if (!canSnip && currentCooldown > 0f)
        {
            currentCooldown -= Time.deltaTime;

            // Плавно опустошаем иконку
            if (cooldownFillImage != null)
            {
                cooldownFillImage.fillAmount = 1f - (currentCooldown / cooldownTime);
            }

            // Когда кулдаун закончился
            if (currentCooldown <= 0f)
            {
                canSnip = true;
                currentCooldown = 0f;
            }
        }
    }

    private IEnumerator WaitAndThenProcessEnemies()
    {
        isCountingDown = true;
        yield return new WaitForSeconds(0.5f);
        if (enemiesInRange.Count >= 2)
        {
            ProcessEnemiesInZone();
        }
        isCountingDown = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") && !enemiesInRange.Contains(other.gameObject))
        {
            enemiesInRange.Add(other.gameObject);
            if (enemiesInRange.Count >= 2 && canSnip && !isCountingDown)
            {
                StartCoroutine(WaitAndThenProcessEnemies());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (enemiesInRange.Contains(other.gameObject))
        {
            enemiesInRange.Remove(other.gameObject);
        }
    }

    private void ExecuteSnip(GameObject targetEnemy)
    {
        Vector3 direction = targetEnemy.transform.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion snipRotation = Quaternion.Euler(0, 0, angle);
        if (scissors != null)
        {
            Instantiate(scissors, targetEnemy.transform.position, snipRotation);
            Debug.Log($"Snipping {targetEnemy.name} at angle: {angle}");
        }
    }

    private void ProcessEnemiesInZone()
    {
        GameObject[] targets = enemiesInRange.ToArray();
        canSnip = false; // Блокируем новые атаки

        foreach (GameObject target in targets)
        {
            if (target != null)
            {
                ExecuteSnip(target);

                EnemyGeneric enemyScript = target.GetComponent<EnemyGeneric>();

                if (enemyScript != null)
                {
                        StartCoroutine(DelayedStun(enemyScript, 0.667f));
                }
            }
        }
        enemiesInRange.Clear();

        // Вместо Invoke теперь запускаем таймер кулдауна
        currentCooldown = cooldownTime;
        if (cooldownFillImage != null) cooldownFillImage.fillAmount = 1f;
    }

    private IEnumerator DelayedStun(EnemyGeneric enemyScript, float delay)
    {
        if (enemyScript != null && enemyScript.gameObject != null)
        {
            cuttedEnemies++;
            yield return new WaitForSeconds(delay);

            if (enemyScript != null && enemyScript.gameObject != null)
            {
                enemyScript.StartCoroutine(enemyScript.Stun());
            }
        }
    }

    private IEnumerator DelayedDestroy(GameObject target, float delay)
    {
        if (target != null)
        {
            cuttedEnemies++;

            yield return new WaitForSeconds(delay);

            if (target != null)
            {
                EnemyGeneric enemyScript = target.GetComponent<EnemyGeneric>();
                if (enemyScript != null)
                {
                    enemyScript.Die();
                }
                else
                {
                    var objScript = FindFirstObjectByType<objective>();
                    if (objScript != null) objScript.CountEnemyDeath();
                    Destroy(target);
                }
            }
        }
    }
}