using UnityEngine;
using UnityEngine.InputSystem; // Для нової системи введення (Escape)
using UnityEngine.SceneManagement; // Для переходу між сценами

public class PauseManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource gameplaySource;
    public AudioSource pauseSource;

    [Header("UI Panel")]
    public GameObject pauseMenuUI;

    [Header("Scene Settings")]
    public string menuSceneName = "Menu"; // Точна назва вашої сцени з меню

    private bool isPaused = false;

    void Start()
    {
        // При запуску сцени переконуємося, що час іде, а меню й музика паузи вимкнені
        Time.timeScale = 1f;

        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (pauseSource != null) pauseSource.Stop();
        if (gameplaySource != null) gameplaySource.Play();
    }

    void Update()
    {
        // Перевірка натискання Escape за допомогою New Input System
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; // Зупиняємо час у грі

        // Ставимо музику гри на паузу та запускаємо музику паузи
        if (gameplaySource != null) gameplaySource.Pause();
        if (pauseSource != null) pauseSource.Play();
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // Відновлюємо час у грі

        // Зупиняємо музику паузи та продовжуємо музику гри з того ж місця
        if (pauseSource != null) pauseSource.Stop();
        if (gameplaySource != null) gameplaySource.UnPause();
    }

    // Метод для кнопки повороту в меню (Back)
    public void GoToMenu()
    {
        // 1. Повертаємо нормальний час у грі перед виходом
        Time.timeScale = 1f;

        // 2. Повністю зупиняємо всі звуки сцени гри
        if (gameplaySource != null) gameplaySource.Stop();
        if (pauseSource != null) pauseSource.Stop();

        // 3. Завантажуємо сцену меню
        SceneManager.LoadScene(menuSceneName);
    }
}