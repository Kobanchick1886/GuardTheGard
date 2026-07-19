using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class ScissorsCombo : MonoBehaviour
{
    public GameObject scissors;
    private List<GameObject> enemiesInRange = new List<GameObject>();
    public bool canSnip = false;
    private bool isCountingDown = false;
    public int cuttedEnemies = 0;

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
        canSnip = false;
        foreach (GameObject target in targets)
        {
            if (target != null)
            {
                ExecuteSnip(target);

                // Достаем скрипт EnemyGeneric, чтобы узнать значение маркера
                EnemyGeneric enemyScript = target.GetComponent<EnemyGeneric>();

                if (enemyScript != null)
                {
                    // Если маркер включен - станим (с задержкой на анимацию ножниц)
                    if (enemyScript.Marker)
                    {
                        StartCoroutine(DelayedStun(enemyScript, 0.667f));
                    }
                    // Если маркер выключен - убиваем насмерть (тоже с задержкой под анимацию)
                    else
                    {
                        StartCoroutine(DelayedDestroy(target, 0.667f));
                    }
                }
            }
        }
        enemiesInRange.Clear();
        Invoke("ResetSnip", 1.0f);
    }

    // Корутина для стана (если Marker == true)
    private IEnumerator DelayedStun(EnemyGeneric enemyScript, float delay)
    {
        if (enemyScript != null && enemyScript.gameObject != null)
        {
            cuttedEnemies++; // Считаем за успешное применение для статы
            yield return new WaitForSeconds(delay);

            if (enemyScript != null && enemyScript.gameObject != null)
            {
                enemyScript.StartCoroutine(enemyScript.Stun());
            }
        }
    }

    // Корутина для убийства (если Marker == false)
    private IEnumerator DelayedDestroy(GameObject target, float delay)
    {
        if (target != null)
        {
            cuttedEnemies++; // Считаем за убийство для статы

            yield return new WaitForSeconds(delay);

            if (target != null)
            {
                // Вызываем метод Die() через скрипт EnemyGeneric, чтобы всё отработало корректно
                EnemyGeneric enemyScript = target.GetComponent<EnemyGeneric>();
                if (enemyScript != null)
                {
                    // Делаем метод Die() публичным в EnemyGeneric (см. ниже) и вызываем его
                    enemyScript.Die();
                }
                else
                {
                    // Фолбэк, если скрипта почему-то нет
                    var objScript = FindFirstObjectByType<objective>();
                    if (objScript != null) objScript.CountEnemyDeath();
                    Destroy(target);
                }
            }
        }
    }

    private void ResetSnip()
    {
        canSnip = true;
    }
}