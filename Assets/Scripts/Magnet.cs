using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Magnet : MonoBehaviour
{
    [SerializeField] private GameObject bullet;

    public Vector3 EnemyPos;

    [Header("Cooldown Settings")]
    public float cooldownTime = 1f;

    [Header("UI Bar Components")]
    public Image cooldownFillImage;       // Bar Fill
    public Image cooldownBorderImage;     // Border (Рамка)
    public Image cooldownBackgroundImage;  // Background (Фон)

    private float currentCooldown = 0f;
    private bool canFire = true;

    private System.Collections.Generic.List<GameObject> enemiesInRange = new System.Collections.Generic.List<GameObject>();

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

                enemiesInRange.RemoveAll(item => item == null);

                if (enemiesInRange.Count > 0)
                {
                    TryShootAtFirstAvailableEnemy();
                }
            }
        }
    }

    // МЕТОД ДЛЯ ПОЛНОЙ ОБНОВЫ ВИЗУАЛА ШКАЛЫ (Fill, Border, Background)
    public void UpdateBarVisuals(Sprite fillSprite, Sprite borderSprite, Sprite bgSprite)
    {
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
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            if (!enemiesInRange.Contains(collision.gameObject))
            {
                enemiesInRange.Add(collision.gameObject);
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
            if (enemiesInRange.Contains(collision.gameObject))
            {
                enemiesInRange.Remove(collision.gameObject);
            }
        }
    }

    private void TryShootAtFirstAvailableEnemy()
    {
        enemiesInRange.RemoveAll(item => item == null);

        GameObject targetEnemy = null;
        foreach (var enemy in enemiesInRange)
        {
            if (enemy != null)
            {
                EnemyGeneric enemyScript = enemy.GetComponent<EnemyGeneric>();
                if (enemyScript == null || !enemyScript.IsStunned())
                {
                    targetEnemy = enemy;
                    break;
                }
            }
        }

        if (targetEnemy != null)
        {
            Shoot(targetEnemy.transform);
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