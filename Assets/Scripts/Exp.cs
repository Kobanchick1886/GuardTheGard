using UnityEngine;

public class Exp : MonoBehaviour
{
    private bool isFlying = false;
    private Transform targetTransform;
    private float flySpeed = 25f;


    public void StartFlying(Transform player)
    {
        targetTransform = player;
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
    }
}