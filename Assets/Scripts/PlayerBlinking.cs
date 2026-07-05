using System.Collections;
using UnityEngine;

public class BlinkController : MonoBehaviour
{
    [Header("Eyes animation objects")]
    [SerializeField] private GameObject normalEyesObject; 
    [SerializeField] private GameObject blinkEyesObject;  

    [Header("Times setting (sec)")]
    [SerializeField] private float minTimeBetweenBlinks = 2f; 
    [SerializeField] private float maxTimeBetweenBlinks = 6f; 
    [SerializeField] private float blinkDuration = 0.15f;    

    private bool isCurrentlyBlinking = false;

    void Start()
    {
        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            isCurrentlyBlinking = false;
            float randomWait = Random.Range(minTimeBetweenBlinks, maxTimeBetweenBlinks);
            yield return new WaitForSeconds(randomWait);

            isCurrentlyBlinking = true;
            yield return new WaitForSeconds(blinkDuration);
        }
    }


    void LateUpdate()
    {
        if (normalEyesObject == null || blinkEyesObject == null) return;

        if (isCurrentlyBlinking)
        {
            blinkEyesObject.SetActive(true);
            normalEyesObject.SetActive(false);
        }
        else
        {
            blinkEyesObject.SetActive(false);
            normalEyesObject.SetActive(true);
        }
    }
}