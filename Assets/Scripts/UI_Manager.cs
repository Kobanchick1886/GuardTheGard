using UnityEngine;
using TMPro; // Essential for TextMeshPro

public class UI_Manager : MonoBehaviour
{
    // This creates the slot in the Inspector
    public TextMeshProUGUI enemyText;
    public TextMeshProUGUI mowerSeconds;

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
            enemyText.text = "Enemies Left: " + objectiveScript.enemiesRemainingInWave.ToString();
        }

        // 2. Update Mower Placement Timer
        if (mowerSeconds != null)
        {
            // Find the active Lawn Mower in the scene
            LawnMower activeMower = Object.FindFirstObjectByType<LawnMower>();

            if (activeMower != null && activeMower.isPlacing)
            {
                // Turn the text ON and update the seconds
                mowerSeconds.enabled = true;

                // Mathf.CeilToInt rounds 1.5 up to 2, 0.5 up to 1, etc., for a clean UI
                int displaySeconds = Mathf.CeilToInt(activeMower.countdownTimer);
                mowerSeconds.text = "Seconds before placement: " + displaySeconds.ToString();
            }
            else
            {
                // Turn the text OFF when the mower is done placing or doesn't exist
                mowerSeconds.enabled = false;
            }
        }
    }
}