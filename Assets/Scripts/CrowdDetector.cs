using System.Collections.Generic;
using System.Collections;
using System.Linq.Expressions;
using UnityEngine;

public class CrowdDetector : MonoBehaviour
{
    public GameObject scissors;
    private List<GameObject> enemiesInRange = new List<GameObject>();
    private bool canSnip = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (!enemiesInRange.Contains(other.gameObject))
            {
                enemiesInRange.Add(other.gameObject);
                CheckForCrowd();
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

    private void CheckForCrowd()
    {
        if (enemiesInRange.Count >= 2 && canSnip)
        {
            StartCoroutine(ExecuteSnip());
        }
    }

    private IEnumerator ExecuteSnip()
    {
        canSnip = false;
        float zRotation = 0f;

        switch (gameObject.name)
        {
            case "Top": zRotation = 0f; break;
            case "Right": zRotation = -90f; break;
            case "Bottom": zRotation = 180f; break;
            case "Left": zRotation = 90f; break;
            default:
                Debug.LogError($"[Setup] GameObject name '{gameObject.name}' doesn't match switch cases!");
                break;
        }

        Quaternion snipRotation = Quaternion.Euler(0, 0, zRotation);

        if (scissors != null)
        {
            yield return new WaitForSecondsRealtime(2f);
            Instantiate(scissors, transform.position, snipRotation);
        }
        KillEnemiesInZone();
        Invoke("ResetSnip", 1.0f);
    }

    private void KillEnemiesInZone()
    {
        int killedCount = 0;
        foreach (GameObject enemy in enemiesInRange.ToArray())
        {
            if (enemy != null)
            {
                var objScript = FindFirstObjectByType<objective>();
                if (objScript != null) objScript.CountEnemyDeath();

                Destroy(enemy);
                killedCount++;
            }
        }
        enemiesInRange.Clear();
    }

    private void ResetSnip()
    {
        canSnip = true;
    }
}