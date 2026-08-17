using UnityEngine;
using UnityEngine.UI;

public class UIButtonManagerSound : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickSound;

    void Awake()
    {
        // Якщо AudioSource не призначено вручну, беремо з цього ж об'єкта
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Автоматично шукаємо всі кнопки на сцені (включаючи приховані)
        AssignSoundToAllButtons();
    }

    public void AssignSoundToAllButtons()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Button btn in allButtons)
        {
            // Видаляємо старі слухачі, щоб звук не дублювався при перезапуску/перепідключенні
            btn.onClick.RemoveListener(PlaySound);

            // Додаємо відтворення звуку на подію OnClick
            btn.onClick.AddListener(PlaySound);
        }
    }

    private void PlaySound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
}