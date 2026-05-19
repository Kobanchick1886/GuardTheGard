using System.Collections.Generic;
using System.Collections;
using System.Linq.Expressions;
using UnityEngine;

public class ScissorsCombo : MonoBehaviour
{
    public GameObject scissors;
    private List<GameObject> enemiesInRange = new List<GameObject>();
    public bool canSnip = false;
    private bool isCountingDown = false;
    public int cuttedEnemies = 0;

    private IEnumerator WaitAndThenKill()
    {
        isCountingDown = true;
        yield return new WaitForSeconds(0.5f);
        if (enemiesInRange.Count >= 2)
        {
            KillEnemiesInZone();
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
                StartCoroutine(WaitAndThenKill());
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

    private void KillEnemiesInZone()
    {
        GameObject[] targets = enemiesInRange.ToArray();
        canSnip = false;
        foreach (GameObject enemy in targets)
        {
            if (enemy != null)
            {
                ExecuteSnip(enemy);
                StartCoroutine(DelayedDestroy(enemy, 0.667f));
            }
        }
        enemiesInRange.Clear();
        Invoke("ResetSnip", 1.0f);
    }

    private IEnumerator DelayedDestroy(GameObject target, float delay)
    {
        if (target != null)
        {
            cuttedEnemies++;
            var objScript = FindFirstObjectByType<objective>();
            if (objScript != null) objScript.CountEnemyDeath();

            yield return new WaitForSeconds(delay);

            if (target != null) Destroy(target);
        }
    }

    private void ResetSnip()
    {
        canSnip = true;
    }


}