using UnityEngine;

public class AnimationAudio : MonoBehaviour
{
    public AudioSource audioSource;

    // Метод, який викликатиме анімація
    public void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            // PlayOneShot дозволяє звукам накладатися один на одного без переривання
            audioSource.PlayOneShot(clip);
        }
    }
}