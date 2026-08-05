using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PowerUpManagement : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuPanel;
    public Button buttonLeft, buttonRight;
    public Image leftCardImage, rightCardImage;
    public Image leftFill, rightFill;
    public Volume blurVolume;

    [Header("Бабушки (Отдельные объекты поверх карточек)")]
    public GameObject leftGrandma;
    public GameObject rightGrandma;

    [System.Serializable]
    public class PowerUpSprites
    {
        public Sprite spriteEN;
        public Sprite spriteUA;
    }

    [Header("Спрайты цельных карточек (Все 6 штук)")]
    public PowerUpSprites sprayerLvl2;
    public PowerUpSprites sprayerLvl4;
    public PowerUpSprites scissorsLvl1;
    public PowerUpSprites scissorsLvl2;
    public PowerUpSprites lawnmowerLvl1;
    public PowerUpSprites lawnmowerLvl2;

    [Header("Settings")]
    public float holdDuration = 2.0f;
    private float currentHoldTime = 0f;
    private bool isLeftSelected = true;
    public bool isMenuActive = false;

    [HideInInspector] public List<string> selectedHistory = new List<string>();

    private PowerUp leftPowerUp;
    private PowerUp rightPowerUp;

    [SerializeField]
    private GameObject LawnMowerPrefab; // Закинь сюда ПРЕФАБ газонокосилки (LawnMower)

    [SerializeField]
    private GameObject scissorsVisualObject;

    private class PowerUp
    {
        public string Name;
        public Action Execute;
        public bool IsOneTime;
        public PowerUpSprites Sprites;

        public PowerUp(string name, Action execute, PowerUpSprites sprites, bool isOneTime = false)
        {
            Name = name;
            Execute = execute;
            Sprites = sprites;
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

    void OnEnable()
    {
        LanguageSwitcher.OnLanguageChanged += UpdateCardTranslations;
    }

    void OnDisable()
    {
        LanguageSwitcher.OnLanguageChanged -= UpdateCardTranslations;
    }

    private void InitializePowerUps()
    {
        // Изначально создаем пустой пул
        powerUpPool = new List<PowerUp>();

        // --- БАЗОВЫЕ УЛУЧШЕНИЯ (Доступны сразу) ---

        // 1. ПШИКАЛКА 2 УРОВЕНЬ
        powerUpPool.Add(new PowerUp("Sprayer Lvl 2", () =>
        {
            Magnet sprayer = FindFirstObjectByType<Magnet>(FindObjectsInactive.Include);
            if (sprayer != null) sprayer.cooldownTime = 4f;

            // Как только взяли 2 уровень, добавляем в пул 4-й уровень
            powerUpPool.Add(new PowerUp("Sprayer Lvl 4", () =>
            {
                Magnet s = FindFirstObjectByType<Magnet>(FindObjectsInactive.Include);
                if (s != null) s.cooldownTime = 3f;
            }, sprayerLvl4, true));

        }, sprayerLvl2, true));

        // 3. НОЖНИЦЫ 1 УРОВЕНЬ
        powerUpPool.Add(new PowerUp("Scissors Lvl 1", () =>
        {
            ScissorsCombo scissors = FindFirstObjectByType<ScissorsCombo>(FindObjectsInactive.Include);
            if (scissors != null)
            {
                scissors.gameObject.SetActive(true);
                scissors.canSnip = true;
            }
            if (scissorsVisualObject != null) scissorsVisualObject.SetActive(true);

            // Как только активировали ножницы, добавляем в пул их прокачку
            powerUpPool.Add(new PowerUp("Scissors Lvl 2", () =>
            {
                ScissorsCombo sc = FindFirstObjectByType<ScissorsCombo>(FindObjectsInactive.Include);
                if (sc != null)
                {
                    sc.cooldownTime -= 1f;
                    Debug.Log("<color=green>Scissors cooldown upgraded to: </color>" + sc.cooldownTime);
                }
            }, scissorsLvl2, true));

        }, scissorsLvl1, true));

        // 5. ГАЗОНОКОСИЛКА 1 УРОВЕНЬ
        powerUpPool.Add(new PowerUp("Lawnmower Lvl 1", () =>
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (LawnMowerPrefab != null && player != null)
            {
                Instantiate(LawnMowerPrefab, player.transform.position, Quaternion.identity);
            }

            // Как только заспавнили косилку, добавляем в пул её ускорение
            powerUpPool.Add(new PowerUp("Lawnmower Lvl 2", () =>
            {
                LawnMower spawnedMower = FindFirstObjectByType<LawnMower>();
                if (spawnedMower != null)
                {
                    spawnedMower.mowerSpeed *= 1.2f;

                    MowerItself actualMower = FindFirstObjectByType<MowerItself>();
                    if (actualMower != null)
                    {
                        // actualMower.speed *= 1.2f; 
                    }
                }
            }, lawnmowerLvl2, true));

        }, lawnmowerLvl1, true));
    }

    void Update()
    {
        if (!isMenuActive) return;

        if (Keyboard.current.aKey.wasPressedThisFrame && !isLeftSelected)
        {
            isLeftSelected = true;
            ResetHold();
            UpdateHoverState();
        }
        if (Keyboard.current.dKey.wasPressedThisFrame && isLeftSelected)
        {
            isLeftSelected = false;
            ResetHold();
            UpdateHoverState();
        }

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
        if (powerUpPool.Count == 0) return;

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

        isLeftSelected = true;
        isMenuActive = true;
        menuPanel.SetActive(true);
        if (blurVolume != null) blurVolume.weight = 1f;
        Time.timeScale = 0f;

        ResetHold();
        UpdateHoverState();
        UpdateCardTranslations();
    }

    private void UpdateHoverState()
    {
        if (leftGrandma != null) leftGrandma.SetActive(isLeftSelected);
        if (rightGrandma != null) rightGrandma.SetActive(!isLeftSelected);

        if (isLeftSelected) buttonLeft.Select();
        else buttonRight.Select();
    }

    private void UpdateCardTranslations()
    {
        if (!isMenuActive) return;
        string currentLang = PlayerPrefs.GetString("SavedLanguage", "English");

        if (leftCardImage != null && leftPowerUp != null)
        {
            leftCardImage.sprite = (currentLang == "Українська") ? leftPowerUp.Sprites.spriteUA : leftPowerUp.Sprites.spriteEN;
        }

        if (rightCardImage != null && rightPowerUp != null)
        {
            rightCardImage.sprite = (currentLang == "Українська") ? rightPowerUp.Sprites.spriteUA : rightPowerUp.Sprites.spriteEN;
        }
    }

    private void ExecuteSelection()
    {
        PowerUp selectedPowerUp = isLeftSelected ? leftPowerUp : rightPowerUp;

        if (selectedPowerUp != null && selectedPowerUp.Execute != null)
        {
            selectedPowerUp.Execute.Invoke();
            selectedHistory.Add(selectedPowerUp.Name);

            if (selectedPowerUp.IsOneTime)
            {
                powerUpPool.Remove(selectedPowerUp);
            }
        }

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

    public void OpenAnalytics()
    {
        try
        {
            string targetPath = Path.Combine(Application.persistentDataPath, "analytics.exe");

            if (File.Exists(targetPath))
            {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.FileName = targetPath;
                startInfo.WorkingDirectory = Application.persistentDataPath;

                System.Diagnostics.Process.Start(startInfo);
                UnityEngine.Debug.Log("<color=green>Успешный запуск аналитики из AppData: </color>" + targetPath);
            }
            else
            {
                UnityEngine.Debug.LogError("<color=red>Файл не найден в AppData!</color> Искал здесь: " + targetPath);
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Ошибка запуска аналитики: " + e.Message);
        }
    }
}