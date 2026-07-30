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
        // 1. При открытии меню подгружаем сохраненные данные 
        if (musicSlider != null) musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (soundsSlider != null) soundsSlider.value = PlayerPrefs.GetFloat("SoundsVolume", 1f);
        if (playerSpeedSlider != null) playerSpeedSlider.value = PlayerPrefs.GetFloat("PlayerSpeed", 5f);
        if (enemySpeedSlider != null) enemySpeedSlider.value = PlayerPrefs.GetFloat("EnemySpeed", 3f);

        // 2. Подписываем ползунки на автоматическое сохранение при любом их движении
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SaveMusic);
        if (soundsSlider != null) soundsSlider.onValueChanged.AddListener(SaveSounds);
        if (playerSpeedSlider != null) playerSpeedSlider.onValueChanged.AddListener(SavePlayerSpeed);
        if (enemySpeedSlider != null) enemySpeedSlider.onValueChanged.AddListener(SaveEnemySpeed);

        // 3. Подвязываем кнопку выхода в главное меню
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
        PlayerPrefs.SetFloat("PlayerSpeed", value);
        PlayerPrefs.Save();
    }

    private void SaveEnemySpeed(float value)
    {
        PlayerPrefs.SetFloat("EnemySpeed", value);
        PlayerPrefs.Save();
    }
}