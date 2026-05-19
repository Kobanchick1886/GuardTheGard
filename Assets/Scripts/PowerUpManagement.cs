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

    // Variables to hold the dynamically chosen functions and names
    private Action leftExecutor;
    private Action rightExecutor;
    private string leftName;
    private string rightName;

    [SerializeField]
    private GameObject LawnMower;
    // --- DATA STRUCTURE FOR POWER-UPS ---
    // This creates an object that holds a Name and an Executable Function
    private class PowerUp
    {
        public string Name;
        public Action Execute;

        public PowerUp(string name, Action execute)
        {
            Name = name;
            Execute = execute;
        }
    }

    // The array/list of available power-ups
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
            new PowerUp("Speed x1.5", () =>
            {
                Movement playerMove = FindFirstObjectByType<Movement>();
                if (playerMove != null) playerMove.UpgradeSpeed(1.2f);
            }),

            new PowerUp("Lawn Mower", () =>
            {
                // Instantiates the Mower prefab directly at the Player's position
                GameObject player = GameObject.FindWithTag("Player");
                if (LawnMower != null && player != null)
                {
                    Instantiate(LawnMower, player.transform.position, Quaternion.identity);
                    Debug.Log("Spawned Lawn Mower at Player.");
                }
            }),

            new PowerUp("Scissors", () =>
            {
                ScissorsCombo scissors = FindFirstObjectByType<ScissorsCombo>(FindObjectsInactive.Include);
                if (scissors != null)
                {
                    scissors.gameObject.SetActive(true);
                    scissors.canSnip = true;
                }
            }),

            // REPLACED SCISSORS RANGE WITH MAIN ATTACK RANGE
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
        // Randomly choose 2 distinct indices from the pool
        int index1 = UnityEngine.Random.Range(0, powerUpPool.Count);
        int index2 = UnityEngine.Random.Range(0, powerUpPool.Count);

        // Ensure they are not the same power-up
        while (index1 == index2)
        {
            index2 = UnityEngine.Random.Range(0, powerUpPool.Count);
        }

        // Map the selected functions to the UI buttons
        leftName = powerUpPool[index1].Name;
        leftExecutor = powerUpPool[index1].Execute;

        rightName = powerUpPool[index2].Name;
        rightExecutor = powerUpPool[index2].Execute;

        // Update the UI Text
        if (textLeft != null) textLeft.text = leftName;
        if (textRight != null) textRight.text = rightName;

        isMenuActive = true;
        menuPanel.SetActive(true);
        if (blurVolume != null) blurVolume.weight = 1f;
        Time.timeScale = 0f;
        ResetHold();
    }

    private void ExecuteSelection()
    {
        // EXECUTING THE FUNCTION BEFORE CLOSING THE MENU
        if (isLeftSelected && leftExecutor != null)
        {
            leftExecutor.Invoke();
            selectedHistory.Add(leftName);
            Debug.Log("<color=green>Executed PowerUp: " + leftName + "</color>");
        }
        else if (!isLeftSelected && rightExecutor != null)
        {
            rightExecutor.Invoke();
            selectedHistory.Add(rightName);
            Debug.Log("<color=green>Executed PowerUp: " + rightName + "</color>");
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