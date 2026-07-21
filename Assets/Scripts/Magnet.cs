using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI; // Обязательно добавляем для работы с UI

public class Magnet : MonoBehaviour
{
    [SerializeField] private GameObject bullet;

    public Vector3 EnemyPos;

    [Header("Cooldown Settings")]
    public float cooldownTime = 1f; // Время перезарядки в секундах
    public Image cooldownFillImage; // Сюда перетащи Bar Fill из Canvas
    private float currentCooldown = 0f;
    private bool canFire = true;

    private void Update()
    {
        // Если способность на кулдауне, крутим таймер
        if (!canFire)
        {
            currentCooldown -= Time.deltaTime;

            // Плавно меняем заполнение картинки от 1 до 0
            if (cooldownFillImage != null)
            {
                cooldownFillImage.fillAmount = 1f - (currentCooldown / cooldownTime);
            }

            // Когда время вышло, способность снова готова
            if (currentCooldown <= 0f)
            {
                canFire = true;
                currentCooldown = 0f;
            }
        }
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
        // Добавили проверку на canFire
        if (collision.CompareTag("Enemy") && canFire)
        {
            GameObject holder = Instantiate(bullet, transform.parent.position, Quaternion.identity);
            Vector3 direction = collision.transform.position - transform.parent.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            holder.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            Bullet[] prop = holder.GetComponentsInChildren<Bullet>();
            foreach (Bullet b in prop)
            {
                b.StartFlying(collision.transform);
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

            // ЗАПУСКАЕМ КУЛДАУН
            canFire = false;
            currentCooldown = cooldownTime;
            if (cooldownFillImage != null) cooldownFillImage.fillAmount = 1f;
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