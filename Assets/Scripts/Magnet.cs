using Unity.VisualScripting;
using UnityEngine;

public class Magnet : MonoBehaviour
{
    [SerializeField] private GameObject bullet;

    public Vector3 EnemyPos;

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
        }
    }

    // --- NEW: RANGE EXTENSION FUNCTION ---
    public void UpgradeRange(float multiplier)
    {
        // Assumes your trigger is a CircleCollider2D. 
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