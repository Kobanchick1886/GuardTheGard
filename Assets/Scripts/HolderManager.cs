using System.Collections;
using UnityEngine;

public class HolderManager : MonoBehaviour
{
    private objective EnemyCounter;
    private bool hasDied = false;
    private EnemyGeneric stun;

    private void Awake()
    {
        EnemyCounter = Object.FindAnyObjectByType<objective>();
    }

    void Update()
    {
        if (transform.childCount < 3 && !hasDied) 
        {
            hasDied = true;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            Destroy(gameObject);
        }

    }
}