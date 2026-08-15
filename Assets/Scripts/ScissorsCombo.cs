using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScissorsCombo : MonoBehaviour
{
    public GameObject scissors;

    // ВАЖНО: Следим за коллайдерами, а не объектами (фикс бага с корешками)
    private List<Collider2D> collidersInRange = new List<Collider2D>();

    public bool canSnip = false;
    private bool isCountingDown = false;
    public int cuttedEnemies = 0;

    [Header("Cooldown Settings")]
    public float cooldownTime = 4f; // Базовое время для 1 уровня

    [Header("UI Bar Components")]
    public Image cooldownFillImage;       // SC_Bar Fill
    public Image cooldownBorderImage;     // SC_Border
    public Image cooldownBackgroundImage; // SC_Background

    private float currentCooldown = 0f;

    private void Update()
    {
        if (!canSnip && currentCooldown > 0f)
        {
            currentCooldown -= Time.deltaTime;

            if (cooldownFillImage != null)
            {
                cooldownFillImage.fillAmount = 1f - (currentCooldown / cooldownTime);
            }

            if (currentCooldown <= 0f)
            {
                canSnip = true;
                currentCooldown = 0f;

                // Таймер вышел - сразу проверяем, есть ли кого резать
                TryStartSnipSequence();
            }
        }
    }

    // МЕТОД ДЛЯ БЕЗОПАСНОГО ОБНОВЛЕНИЯ КУЛДАУНА И ВИЗУАЛА
    public void ApplyUpgrade(float newCooldownTime, Sprite fillSprite, Sprite borderSprite, Sprite bgSprite)
    {
        cooldownTime = newCooldownTime;

        // Фикс "супер кулдауна": обрезаем таймер под новое значение
        if (currentCooldown > cooldownTime)
        {
            currentCooldown = cooldownTime;
        }

        if (cooldownFillImage != null && fillSprite != null) cooldownFillImage.sprite = fillSprite;
        if (cooldownBorderImage != null && borderSprite != null) cooldownBorderImage.sprite = borderSprite;
        if (cooldownBackgroundImage != null && bgSprite != null) cooldownBackgroundImage.sprite = bgSprite;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (!collidersInRange.Contains(other))
            {
                collidersInRange.Add(other);
            }

            TryStartSnipSequence();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (!collidersInRange.Contains(other))
            {
                collidersInRange.Add(other);
            }

            TryStartSnipSequence();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (collidersInRange.Contains(other))
            {
                collidersInRange.Remove(other);
            }
        }
    }

    // Метод, который фильтрует корешки и возвращает только уникальных врагов
    private List<Transform> GetUniqueEnemies()
    {
        collidersInRange.RemoveAll(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        List<Transform> uniqueEnemies = new List<Transform>();

        foreach (var col in collidersInRange)
        {
            EnemyGeneric enemyScript = col.GetComponentInParent<EnemyGeneric>();

            if (enemyScript != null)
            {
                // Игнорируем врагов в стане и избегаем дублирования
                if (!enemyScript.IsStunned() && !uniqueEnemies.Contains(enemyScript.transform))
                {
                    uniqueEnemies.Add(enemyScript.transform);
                }
            }
            else
            {
                // Резервный вариант для объектов без скрипта, но с тегом Enemy
                if (!uniqueEnemies.Contains(col.transform))
                {
                    uniqueEnemies.Add(col.transform);
                }
            }
        }

        return uniqueEnemies;
    }

    private void TryStartSnipSequence()
    {
        if (canSnip && !isCountingDown)
        {
            // Проверяем, набралось ли минимум 2 УНИКАЛЬНЫХ врага
            if (GetUniqueEnemies().Count >= 2)
            {
                StartCoroutine(WaitAndThenProcessEnemies());
            }
        }
    }

    private IEnumerator WaitAndThenProcessEnemies()
    {
        isCountingDown = true;
        yield return new WaitForSeconds(0.5f);

        List<Transform> targets = GetUniqueEnemies();

        // Повторная проверка после ожидания
        if (targets.Count >= 2 && canSnip)
        {
            ProcessEnemiesInZone(targets);
        }

        isCountingDown = false;
    }

    private void ProcessEnemiesInZone(List<Transform> targets)
    {
        canSnip = false;

        foreach (Transform target in targets)
        {
            ExecuteSnip(target);

            EnemyGeneric enemyScript = target.GetComponent<EnemyGeneric>();
            if (enemyScript != null)
            {
                StartCoroutine(DelayedStun(enemyScript, 0.667f));
            }
        }

        currentCooldown = cooldownTime;
        if (cooldownFillImage != null) cooldownFillImage.fillAmount = 1f;
    }

    private void ExecuteSnip(Transform targetTransform)
    {
        Vector3 direction = targetTransform.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion snipRotation = Quaternion.Euler(0, 0, angle);

        if (scissors != null)
        {
            Instantiate(scissors, targetTransform.position, snipRotation);
            Debug.Log($"Snipping {targetTransform.name} at angle: {angle}");
        }
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

   
}