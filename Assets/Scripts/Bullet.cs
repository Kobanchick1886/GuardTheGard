using UnityEngine;

public class Bullet : MonoBehaviour
{
    private bool isFlying = false;
    private Transform targetTransform;
    private float flySpeed = 20f;
    
    // Update is called once per frame
    public void StartFlying(Transform enemy)
    {
        targetTransform = enemy;
        isFlying = true;
    }

    void Update()
    {
        if (isFlying && targetTransform != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetTransform.position,
                flySpeed * Time.deltaTime
            );
        }
        if (targetTransform ==null)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy")) {
            if (collision.TryGetComponent<EnemyGeneric>(out EnemyGeneric enemy))
            {
                enemy.StartCoroutine(enemy.Stun());
            }
            Destroy(gameObject);
            
        }
    }
}
