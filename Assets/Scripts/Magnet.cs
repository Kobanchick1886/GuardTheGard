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

    [Header("Multiplayer Charges Settings")]
    public int maxCharges = 5;
    public int currentCharges = 5;
    public float shotCooldown = 0.6f;
    private float shotCooldownTimer = 0f;
    private bool isMultiplayerMode = false;

    // ВАЖНО: Теперь мы отслеживаем не GameObject, а конкретные Collider2D.
    // Это спасает от бага, когда удаление/выключение дочернего коллайдера (корешка)
    // заставляло Пшикалку "забыть" про всего врага.
    private List<Collider2D> collidersInRange = new List<Collider2D>();

    private void Start()
    {
        isMultiplayerMode = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SampleSceneMultiplayer";
        if (isMultiplayerMode)
        {
            currentCharges = maxCharges;
            canFire = true;
            UpdateChargesUI();
        }
    }

    private void Update()
    {
        if (!isMultiplayerMode)
        {
            // === РЕЖИМ ОДИНОЧНОЙ ИГРЫ (SampleScene) - БЕЗ ИЗМЕНЕНИЙ ===
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
        else
        {
            // === РЕЖИМ МУЛЬТИПЛЕЕРА (SampleSceneMultiplayer) ===
            // Заряды НЕ восстанавливаются сами по времени!
            if (shotCooldownTimer > 0f)
            {
                shotCooldownTimer -= Time.deltaTime;
                if (shotCooldownTimer <= 0f)
                {
                    if (currentCharges > 0)
                    {
                        canFire = true;
                        TryShootAtFirstAvailableEnemy();
                    }
                    else
                    {
                        canFire = false;
                    }
                }
            }
            UpdateChargesUI();
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

        if (!isMultiplayerMode)
        {
            canFire = false;
            currentCooldown = cooldownTime;
            if (cooldownFillImage != null) cooldownFillImage.fillAmount = 1f;
        }
        else
        {
            currentCharges--;
            canFire = false;
            shotCooldownTimer = shotCooldown;
            UpdateChargesUI();
            Debug.Log($"<color=cyan>[Multiplayer Spray] {transform.parent.name} совершил выстрел! Зарядов осталось: {currentCharges}/{maxCharges}</color>");
        }
    }

    public bool CanAddCharges()
    {
        return isMultiplayerMode && currentCharges < maxCharges;
    }

    public int GetMissingCharges()
    {
        return isMultiplayerMode ? (maxCharges - currentCharges) : 0;
    }

    public void AddCharges(int amount)
    {
        if (!isMultiplayerMode || amount <= 0) return;
        currentCharges = Mathf.Clamp(currentCharges + amount, 0, maxCharges);
        if (currentCharges > 0 && shotCooldownTimer <= 0f)
        {
            canFire = true;
            TryShootAtFirstAvailableEnemy();
        }
        UpdateChargesUI();
        Debug.Log($"<color=green>[Multiplayer Spray] {transform.parent.name} пополнил пшикалку! Зарядов: {currentCharges}/{maxCharges}</color>");
    }

    public void UpdateChargesUI()
    {
        if (cooldownFillImage != null && isMultiplayerMode)
        {
            cooldownFillImage.fillAmount = maxCharges > 0 ? (float)currentCharges / maxCharges : 0f;
        }
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