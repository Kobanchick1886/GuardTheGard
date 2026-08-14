using UnityEngine;
using TMPro;
using UnityEngine.UI; // Обязательно добавляем для работы с RawImage

public class UI_Manager : MonoBehaviour
{
    public TextMeshProUGUI enemyText;

    [Header("Mower Countdown UI")]
    public RawImage mowerCountdownImage;
    public Texture texNum3;
    public Texture texNum2;
    public Texture texNum1;

    private objective objectiveScript;

    void Start()
    {
        objectiveScript = Object.FindAnyObjectByType<objective>();
    }

    void Update()
    {
        // 1. Update Enemy Text
        if (objectiveScript != null && enemyText != null)
        {
            enemyText.text = objectiveScript.enemiesRemainingInWave.ToString() + "/" + (8 * objectiveScript.multiplier);
        }

        // 2. Update Mower Placement Timer
        if (mowerCountdownImage != null)
        {
            // Ищем активную газонокосилку на сцене
            LawnMower activeMower = Object.FindFirstObjectByType<LawnMower>();

            if (activeMower != null && activeMower.isPlacing)
            {
                // Включаем отображение картинки
                mowerCountdownImage.enabled = true;

                // Округляем время вверх
                int displaySeconds = Mathf.CeilToInt(activeMower.countdownTimer);

                // Подставляем нужную текстуру в зависимости от оставшегося времени
                if (displaySeconds >= 3) mowerCountdownImage.texture = texNum3;
                else if (displaySeconds == 2) mowerCountdownImage.texture = texNum2;
                else if (displaySeconds == 1) mowerCountdownImage.texture = texNum1;
                else mowerCountdownImage.texture = null;
            }
            else
            {
                // Прячем картинку, когда косилка не устанавливается
                mowerCountdownImage.enabled = false;
            }
        }
    }
}