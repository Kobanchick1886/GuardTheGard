using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("Sliders Reference")]
    public Slider musicSlider;
    public Slider soundsSlider;
    public Slider playerSpeedSlider;
    public Slider enemySpeedSlider;

    [Header("Buttons")]
    public Button backButton; // Ссылка на кнопку выхода

    void Start()
    {
        // 1. ПРИНУДИТЕЛЬНО задаем лимиты для ползунков скорости
        if (playerSpeedSlider != null)
        {
            playerSpeedSlider.minValue = 50f;
            playerSpeedSlider.maxValue = 250f;
        }

        if (enemySpeedSlider != null)
        {
            enemySpeedSlider.minValue = 3f;
            enemySpeedSlider.maxValue = 15f;
        }

        // 2. Подгружаем сохраненные данные (если их нет, ставим дефолты)
        if (musicSlider != null) musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (soundsSlider != null) soundsSlider.value = PlayerPrefs.GetFloat("SoundsVolume", 1f);
        if (playerSpeedSlider != null) playerSpeedSlider.value = PlayerPrefs.GetFloat("Speed_Player", 120f);
        if (enemySpeedSlider != null) enemySpeedSlider.value = PlayerPrefs.GetFloat("Speed_Enemy", 8f);

        // 3. Подписываем ползунки на автоматическое сохранение при любом их движении
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SaveMusic);
        if (soundsSlider != null) soundsSlider.onValueChanged.AddListener(SaveSounds);
        if (playerSpeedSlider != null) playerSpeedSlider.onValueChanged.AddListener(SavePlayerSpeed);
        if (enemySpeedSlider != null) enemySpeedSlider.onValueChanged.AddListener(SaveEnemySpeed);

        // 4. Подвязываем кнопку выхода в главное меню
        if (backButton != null) backButton.onClick.AddListener(CloseOptions);
    }

    // --- Логика интерфейса ---
    private void CloseOptions()
    {
        // Просто выключаем объект OptionsPanel. 
        // Главное меню находится под ним, поэтому оно сразу станет доступно.
        gameObject.SetActive(false);
    }

    // --- Логика сохранения (записывает данные на жесткий диск на лету) ---
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

    private void SavePlayerSpeed(float value)
    {
        PlayerPrefs.SetFloat("Speed_Player", value);
        PlayerPrefs.Save();
    }

    private void SaveEnemySpeed(float value)
    {
        PlayerPrefs.SetFloat("Speed_Enemy", value);
        PlayerPrefs.Save();
    }
}