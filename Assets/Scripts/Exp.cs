using UnityEngine;

public class Exp : MonoBehaviour
{
    private bool isFlying = false;
    private Transform targetTransform;
    private float flySpeed = 25f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private float soundVolume = 1f;

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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);
            }
        }
    }
}