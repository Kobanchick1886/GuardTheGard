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

    [Header("UI Bars (Шкалы на экране)")]
    public GameObject scissorsUI; // <--- НОВОЕ: Сюда закинь объект шкалы ножниц с Канваса

    [Header("Бабушки (Отдельные объекты поверх карточек)")]
    public GameObject leftGrandma;
    public GameObject rightGrandma;

    [System.Serializable]
    public class PowerUpSprites
    {
        public Sprite spriteEN;
        public Sprite spriteUA;
    }

    // СТРУКТУРА ДЛЯ ТРЕХ СПРАЙТОВ ШКАЛЫ (Fill, Border, Background)
    [System.Serializable]
    public struct CooldownBarSet
    {
        public Sprite fillSprite;
        public Sprite borderSprite;
        public Sprite backgroundSprite;
    }

    [Header("Спрайты цельных карточек (Все 6 штук)")]
    public PowerUpSprites sprayerLvl2;
    public PowerUpSprites sprayerLvl4;
    public PowerUpSprites scissorsLvl1;
    public PowerUpSprites scissorsLvl2;
    public PowerUpSprites lawnmowerLvl1;
    public PowerUpSprites lawnmowerLvl2;

    [Header("Спрайты шкал кулдауна (Комплекты 3 в 1)")]
    public CooldownBarSet sprayerLvl2Bar;
    public CooldownBarSet sprayerLvl4Bar;
    public CooldownBarSet scissorsLvl2Bar;

    [Header("Settings")]
    public float holdDuration = 2.0f;
    private float currentHoldTime = 0f;
    private bool isLeftSelected = true;
    public bool isMenuActive = false;

    [HideInInspector] public List<string> selectedHistory = new List<string>();

    private PowerUp leftPowerUp;
    private PowerUp rightPowerUp;

    [SerializeField]
    private GameObject LawnMowerPrefab;

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

        // Прячем шкалу ножниц при запуске уровня
        if (scissorsUI != null) scissorsUI.SetActive(false);

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
        powerUpPool = new List<PowerUp>();

        // 1. ПШИКАЛКА 2 УРОВЕНЬ (КД падает с 3с до 2с)
        powerUpPool.Add(new PowerUp("Sprayer Lvl 2", () =>
        {
            Magnet sprayer = FindFirstObjectByType<Magnet>(FindObjectsInactive.Include);
            if (sprayer != null)
            {
                sprayer.ApplyUpgrade(2f, sprayerLvl2Bar.fillSprite, sprayerLvl2Bar.borderSprite, sprayerLvl2Bar.backgroundSprite);
            }

            // ПШИКАЛКА 4 УРОВЕНЬ (КД падает с 2с до 1с)
            powerUpPool.Add(new PowerUp("Sprayer Lvl 4", () =>
            {
                Magnet s = FindFirstObjectByType<Magnet>(FindObjectsInactive.Include);
                if (s != null)
                {
                    s.ApplyUpgrade(1f, sprayerLvl4Bar.fillSprite, sprayerLvl4Bar.borderSprite, sprayerLvl4Bar.backgroundSprite);
                }
            }, sprayerLvl4, true));

        }, sprayerLvl2, true));

        // 3. НОЖНИЦЫ 1 УРОВЕНЬ (Активация, базовый КД 4с)
        powerUpPool.Add(new PowerUp("Scissors Lvl 1", () =>
        {
            ScissorsCombo scissors = FindFirstObjectByType<ScissorsCombo>(FindObjectsInactive.Include);
            if (scissors != null)
            {
                scissors.gameObject.SetActive(true);
                scissors.canSnip = true;
                scissors.cooldownTime = 4f;
            }
            if (scissorsVisualObject != null) scissorsVisualObject.SetActive(true);

            // Включаем шкалу ножниц в интерфейсе, так как мы их только что получили
            if (scissorsUI != null) scissorsUI.SetActive(true);

            // НОЖНИЦЫ 2 УРОВЕНЬ (КД падает с 4с до 3с)
            powerUpPool.Add(new PowerUp("Scissors Lvl 2", () =>
            {
                ScissorsCombo sc = FindFirstObjectByType<ScissorsCombo>(FindObjectsInactive.Include);
                if (sc != null)
                {
                    sc.ApplyUpgrade(3f, scissorsLvl2Bar.fillSprite, scissorsLvl2Bar.borderSprite, scissorsLvl2Bar.backgroundSprite);
                }
            }, scissorsLvl2, true));

        }, scissorsLvl1, true));

        // 5. ГАЗОНОКОСИЛКА 1 УРОВЕНЬ (Спавн)
        powerUpPool.Add(new PowerUp("Lawnmower Lvl 1", () =>
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (LawnMowerPrefab != null && player != null)
            {
                Instantiate(LawnMowerPrefab, player.transform.position, Quaternion.identity);
            }

            // ГАЗОНОКОСИЛКА 2 УРОВЕНЬ (+20% к скорости)
            powerUpPool.Add(new PowerUp("Lawnmower Lvl 2", () =>
            {
                LawnMower spawnedMower = FindFirstObjectByType<LawnMower>();
                if (spawnedMower != null)
                {
                    spawnedMower.mowerSpeed *= 1.2f;
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

    private void ResetHold()
    {
        currentHoldTime = 0f;
        if (leftFill != null) leftFill.fillAmount = 0f;
        if (rightFill != null) rightFill.fillAmount = 0f;
    }
}