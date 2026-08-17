using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class OptionsMenu : MonoBehaviour
{
    [Header("Audio Mixer Settings")]
    public AudioMixer mainMixer;

    [Header("Sliders Reference")]
    public Slider musicSlider;
    public Slider soundsSlider;

    [Header("Difficulty Settings")]
    public Button prevDifficultyButton;
    public Button nextDifficultyButton;
    public TextMeshProUGUI difficultyText;

    
    private float[] playerSpeeds = { 35f, 40f, 50f, 62f, 70f, 80f };
    private float[] enemySpeeds = { 1f, 1.2f, 1.4f, 1.62f, 2f, 4f }; 

    [Header("Buttons")]
    public Button backButton;

    // Массивы с переводами
    private string[] difficultyNamesEN = { "Very Easy", "Easy", "Normal", "Hard", "Very Hard", "Extreme" };
    private string[] difficultyNamesUA = { "Дуже легко", "Легко", "Нормально", "Складно", "Дуже складно", "Екстремально" };

    private int currentDifficultyIndex = 1;

    void Start()
    {
        currentDifficultyIndex = PlayerPrefs.GetInt("DifficultyIndex", 1);
        ApplyAndSaveDifficulty();

        float musicVal = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float soundsVal = PlayerPrefs.GetFloat("SoundsVolume", 1f);

        if (musicSlider != null)
        {
            musicSlider.value = musicVal;
            SetMusicVolume(musicVal);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (soundsSlider != null)
        {
            soundsSlider.value = soundsVal;
            SetSoundsVolume(soundsVal);
            soundsSlider.onValueChanged.AddListener(SetSoundsVolume);
        }

        if (prevDifficultyButton != null) prevDifficultyButton.onClick.AddListener(PrevDifficulty);
        if (nextDifficultyButton != null) nextDifficultyButton.onClick.AddListener(NextDifficulty);
        if (backButton != null) backButton.onClick.AddListener(CloseOptions);
    }
   private void SetMusicVolume(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0.0001f, 1f);
        float db = Mathf.Log10(clampedValue) * 20f;

        if (mainMixer != null)
        {
            bool result = mainMixer.SetFloat("MusicVolume", db);
            if (!result) Debug.LogError("Параметр 'MusicVolume' не знайдено в Exposed Parameters AudioMixer!");
        }

        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    private void SetSoundsVolume(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0.0001f, 1f);
        float db = Mathf.Log10(clampedValue) * 20f;

        if (mainMixer != null)
        {
            mainMixer.SetFloat("SoundsVolume", db);
        }

        PlayerPrefs.SetFloat("SoundsVolume", value);
        PlayerPrefs.Save();
    }

    // Подписываемся на смену языка для моментального обновления UI
    void OnEnable()
    {
        LanguageSwitcher.OnLanguageChanged += UpdateDifficultyUI;
    }

    void OnDisable()
    {
        LanguageSwitcher.OnLanguageChanged -= UpdateDifficultyUI;
    }

    private void PrevDifficulty()
    {
        if (currentDifficultyIndex > 0)
        {
            currentDifficultyIndex--;
            ApplyAndSaveDifficulty();
        }
    }

    private void NextDifficulty()
    {
        if (currentDifficultyIndex < difficultyNamesEN.Length - 1)
        {
            currentDifficultyIndex++;
            ApplyAndSaveDifficulty();
        }
    }

    private void ApplyAndSaveDifficulty()
    {
        UpdateDifficultyUI();
        PlayerPrefs.SetInt("DifficultyIndex", currentDifficultyIndex);

        // Берем конкретные значения скорости из массивов по текущему индексу сложности
        float finalPlayerSpeed = playerSpeeds[currentDifficultyIndex];
        float finalEnemySpeed = enemySpeeds[currentDifficultyIndex];

        PlayerPrefs.SetFloat("Speed_Player", finalPlayerSpeed);
        PlayerPrefs.SetFloat("Speed_Enemy", finalEnemySpeed);
        PlayerPrefs.Save();
    }

    private void UpdateDifficultyUI()
    {
        if (difficultyText != null)
        {
            string currentLang = PlayerPrefs.GetString("SavedLanguage", "English");

            if (currentLang == "Українська")
            {
                difficultyText.text = difficultyNamesUA[currentDifficultyIndex];
            }
            else // English
            {
                difficultyText.text = difficultyNamesEN[currentDifficultyIndex];
            }
        }

        if (prevDifficultyButton != null) prevDifficultyButton.interactable = (currentDifficultyIndex > 0);
        if (nextDifficultyButton != null) nextDifficultyButton.interactable = (currentDifficultyIndex < difficultyNamesEN.Length - 1);
    }

    private void CloseOptions()
    {
        gameObject.SetActive(false);
    }

    private void SaveMusic(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    private void SaveSounds(float value)
    {
        PlayerPrefs.SetFloat("SoundsVolume", value);
        PlayerPrefs.Save();
    }
}