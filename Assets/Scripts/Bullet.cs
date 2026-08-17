using UnityEngine;

public class Bullet : MonoBehaviour
{
    private bool isFlying = false;
    private Transform targetTransform;
    private float flySpeed = 20f;

    [Header("Audio Settings")]
    public AudioClip shootSound;
    public UnityEngine.Audio.AudioMixerGroup sfxGroup;

    // Update is called once per frame
    public void StartFlying(Transform enemy)
    {
        targetTransform = enemy;
        isFlying = true;

        if (shootSound != null)
        {
            PlaySFX(shootSound, transform.position);
        }
    }
    private void PlaySFX(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        // Створюємо тимчасовий об'єкт під звук
        GameObject tempGO = new GameObject("TempSFX");
        tempGO.transform.position = position;

        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = clip;
        aSource.outputAudioMixerGroup = sfxGroup; // ПРИВ'ЯЗУЄМО ДО МІКШЕРА!
        aSource.Play();

        Destroy(tempGO, clip.length); // Видаляємо після завершення звуку
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
