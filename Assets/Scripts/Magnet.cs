using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Magnet : MonoBehaviour
{
    [SerializeField] private GameObject bullet;

    public Vector3 EnemyPos;

    [Header("Cooldown Settings")]
    public float cooldownTime = 3f; // Базовое время для 1 уровня

    [Header("UI Bar Components")]
    public Image cooldownFillImage;       // Bar Fill
    public Image cooldownBorderImage;     // Border (Рамка)
    public Image cooldownBackgroundImage; // Background (Фон)

    private float currentCooldown = 0f;
    private bool canFire = true;

    // ВАЖНО: Теперь мы отслеживаем не GameObject, а конкретные Collider2D.
    // Это спасает от бага, когда удаление/выключение дочернего коллайдера (корешка)
    // заставляло Пшикалку "забыть" про всего врага.
    private List<Collider2D> collidersInRange = new List<Collider2D>();

    private void Update()
    {
        if (!canFire)
        {
            currentCooldown -= Time.deltaTime;

            if (cooldownFillImage != null)
            {
                cooldownFillImage.fillAmount = 1f - (currentCooldown / cooldownTime);
            }

            if (currentCooldown <= 0f)
            {
                canFire = true;
                currentCooldown = 0f;

                // Если кулдаун спал - пробуем выстрелить сразу
                TryShootAtFirstAvailableEnemy();
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

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("EXP"))
        {
            if (collision.TryGetComponent<Exp>(out Exp orb))
            {
                orb.StartFlying(transform.parent);
            }
        }
        else if (collision.CompareTag("Enemy"))
        {
            // Подстраховка: если враг чудом оказался в зоне, но не в списке
            if (!collidersInRange.Contains(collision))
            {
                collidersInRange.Add(collision);
            }

            if (canFire)
            {
                TryShootAtFirstAvailableEnemy();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            if (!collidersInRange.Contains(collision))
            {
                collidersInRange.Add(collision);
            }

            if (canFire)
            {
                TryShootAtFirstAvailableEnemy();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            if (collidersInRange.Contains(collision))
            {
                collidersInRange.Remove(collision);
            }
        }
    }

    private void TryShootAtFirstAvailableEnemy()
    {
        // Очищаем список от уничтоженных (null) или выключенных (enabled = false) коллайдеров корешков
        collidersInRange.RemoveAll(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);

        Transform targetTransform = null;

        foreach (var col in collidersInRange)
        {
            // Ищем скрипт на самом коллайдере ИЛИ на его родителе (EnemyGeneric)
            EnemyGeneric enemyScript = col.GetComponentInParent<EnemyGeneric>();

            // Если это враг и он НЕ в стане (или это объект без скрипта, но с тегом Enemy)
            if (enemyScript == null || !enemyScript.IsStunned())
            {
                // Целимся в родительский объект (центр врага), а не в его отдельный корешок
                targetTransform = enemyScript != null ? enemyScript.transform : col.transform;
                break;
            }
        }

        if (targetTransform != null)
        {
            Shoot(targetTransform);
        }
    }

    private void Shoot(Transform targetTransform)
    {
        GameObject holder = Instantiate(bullet, transform.parent.position, Quaternion.identity);
        Vector3 direction = targetTransform.position - transform.parent.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        holder.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        Bullet[] prop = holder.GetComponentsInChildren<Bullet>();
        foreach (Bullet b in prop)
        {
            b.StartFlying(targetTransform);
        }

        if (holder.transform.childCount < 3)
        {
            for (int i = holder.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = holder.transform.GetChild(i).gameObject;
                Destroy(child);
            }
            Destroy(holder);
        }

        canFire = false;
        currentCooldown = cooldownTime;
        if (cooldownFillImage != null) cooldownFillImage.fillAmount = 1f;
    }

    public void UpgradeRange(float multiplier)
    {
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.radius *= multiplier;
            Debug.Log("<color=orange>Magnet Range upgraded to: " + col.radius + "</color>");
        }
        else
        {
            Debug.LogWarning("Magnet does not have a CircleCollider2D attached!");
        }
    }
}