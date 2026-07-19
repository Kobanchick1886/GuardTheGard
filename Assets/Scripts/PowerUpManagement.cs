using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using TMPro;

public class PowerUpManagement : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuPanel;
    public Button buttonLeft, buttonRight;
    public Image leftFill, rightFill;
    public TextMeshProUGUI textLeft, textRight;
    public Volume blurVolume;

    [Header("Settings")]
    public float holdDuration = 2.0f;
    private float currentHoldTime = 0f;
    private bool isLeftSelected = true;
    public bool isMenuActive = false;

    // Analytics tracking for your CSV
    [HideInInspector] public List<string> selectedHistory = new List<string>();

    // Сохраняем сами объекты улучшений, чтобы знать, что удалять
    private PowerUp leftPowerUp;
    private PowerUp rightPowerUp;

    [SerializeField]
    private GameObject LawnMower;

    [SerializeField]
    private GameObject scissorsVisualObject;

    // --- DATA STRUCTURE FOR POWER-UPS ---
    private class PowerUp
    {
        public string Name;
        public Action Execute;
        public bool IsOneTime; // Новый флаг для одноразовых апгрейдов

        // По умолчанию IsOneTime = false, если не указано иное
        public PowerUp(string name, Action execute, bool isOneTime = false)
        {
            Name = name;
            Execute = execute;
            IsOneTime = isOneTime;
        }
    }

    private List<PowerUp> powerUpPool;

    void Awake()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (blurVolume != null) blurVolume.weight = 0f;

        InitializePowerUps();
    }

    // --- THE POWER-UP POOL ---
    private void InitializePowerUps()
    {
        powerUpPool = new List<PowerUp>
        {
            // Многоразовое
            new PowerUp("Lawn Mower", () =>
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (LawnMower != null && player != null)
                {
                    Instantiate(LawnMower, player.transform.position, Quaternion.identity);
                    Debug.Log("Spawned Lawn Mower at Player.");
                }
            }),

            // ОДНОРАЗОВОЕ (добавили true в конце)
            new PowerUp("Scissors", () =>
            {
                ScissorsCombo scissors = FindFirstObjectByType<ScissorsCombo>(FindObjectsInactive.Include);
                if (scissors != null)
                {
                    scissors.gameObject.SetActive(true);
                    scissors.canSnip = true;
                }

                if (scissorsVisualObject != null) {
                    scissorsVisualObject.gameObject.SetActive(true);
                }
            }, true),

            // Многоразовое
            new PowerUp("Attack Range x1.5", () =>
            {
                Magnet mainAttack = FindFirstObjectByType<Magnet>();
                if (mainAttack != null)
                {
                    mainAttack.UpgradeRange(1.5f);
                }
            })
        };
    }

    void Update()
    {
        if (!isMenuActive) return;

        // Navigation
        if (Keyboard.current.aKey.wasPressedThisFrame) { isLeftSelected = true; ResetHold(); }
        if (Keyboard.current.dKey.wasPressedThisFrame) { isLeftSelected = false; ResetHold(); }

        if (isLeftSelected) buttonLeft.Select();
        else buttonRight.Select();

        // Hold Logic
        bool isHolding = (isLeftSelected && Keyboard.current.aKey.isPressed) ||
                         (!isLeftSelected && Keyboard.current.dKey.isPressed);

        if (isHolding)
        {
            currentHoldTime += Time.unscaledDeltaTime;
            float progress = currentHoldTime / holdDuration;

            if (isLeftSelected && leftFill != null) leftFill.fillAmount = progress;
            else if (!isLeftSelected && rightFill != null) rightFill.fillAmount = progress;

            if (currentHoldTime >= holdDuration)
            {
                ExecuteSelection();
            }
        }
        else
        {
            ResetHold();
        }
    }

    public void OpenUpgradeMenu()
    {
        if (powerUpPool.Count == 0)
        {
            Debug.LogWarning("No more power-ups in the pool!");
            return;
        }

        int index1 = UnityEngine.Random.Range(0, powerUpPool.Count);
        int index2 = index1;

        if (powerUpPool.Count > 1)
        {
            while (index1 == index2)
            {
                index2 = UnityEngine.Random.Range(0, powerUpPool.Count);
            }
        }

        leftPowerUp = powerUpPool[index1];
        rightPowerUp = powerUpPool[index2];

        if (textLeft != null) textLeft.text = leftPowerUp.Name;
        if (textRight != null) textRight.text = rightPowerUp.Name;

        isMenuActive = true;
        menuPanel.SetActive(true);
        if (blurVolume != null) blurVolume.weight = 1f;
        Time.timeScale = 0f;
        ResetHold();
    }

    private void ExecuteSelection()
    {
        PowerUp selectedPowerUp = isLeftSelected ? leftPowerUp : rightPowerUp;

        if (selectedPowerUp != null && selectedPowerUp.Execute != null)
        {
            selectedPowerUp.Execute.Invoke();
            selectedHistory.Add(selectedPowerUp.Name);
            Debug.Log("<color=green>Executed PowerUp: " + selectedPowerUp.Name + "</color>");

            if (selectedPowerUp.IsOneTime)
            {
                powerUpPool.Remove(selectedPowerUp);
                Debug.Log($"<color=orange>{selectedPowerUp.Name} was removed from the pool.</color>");
            }
        }

        // Closing the menu and resuming the game
        if (blurVolume != null) blurVolume.weight = 0f;
        isMenuActive = false;
        Time.timeScale = 1f;
        menuPanel.SetActive(false);
    }

    private void ResetHold()
    {
        currentHoldTime = 0f;
        if (leftFill != null) leftFill.fillAmount = 0f;
        if (rightFill != null) rightFill.fillAmount = 0f;
    }
}