using System.Collections;
using UnityEngine;

public class MowerItself : MonoBehaviour
{   
    private bool isNotFirstTime = false;
    public int smashedEnemies = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator turn()
    {   
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = transform.rotation * Quaternion.Euler(0, 0, 180f);
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            elapsed += Time.deltaTime;
            yield return null; 
        }
        transform.rotation = targetRotation;
    }

    IEnumerator snap()
    {
            yield return new WaitForSeconds(2);
            isNotFirstTime = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("base"))
        {
            if (isNotFirstTime)
            {
                StartCoroutine(turn());
            }
        }
        else { StartCoroutine(snap());}
        if (collision.CompareTag("Enemy"))
        {
            smashedEnemies++;
            var objScript = FindFirstObjectByType<objective>();
            if (objScript != null) objScript.CountEnemyDeath();
            Destroy(collision.gameObject);
        }
    }
}
