using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // <--- Нужно для событий

public class LanguageSwitcher : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI valueText;
    public Button leftButton;
    public Button rightButton;

    [Header("Settings")]
    private List<string> languages = new List<string> { "English", "Українська" };
    private int currentIndex = 0;

    // --- СОБЫТИЕ, КОТОРОЕ БУДЕТ ОПОВЕЩАТЬ ВСЕ КАРТИНКИ ОБ ИЗМЕНЕНИИ ЯЗЫКА ---
    public static event Action OnLanguageChanged;

    void Start()
    {
        leftButton.onClick.AddListener(PrevLanguage);
        rightButton.onClick.AddListener(NextLanguage);

        string savedLang = PlayerPrefs.GetString("SavedLanguage", "English");
        int savedIndex = languages.IndexOf(savedLang);

        if (savedIndex != -1) currentIndex = savedIndex;
        else currentIndex = 0;

        UpdateUI();
    }

    public void NextLanguage()
    {
        currentIndex++;
        if (currentIndex >= languages.Count) currentIndex = 0;
        UpdateUI();
    }

    public void PrevLanguage()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = languages.Count - 1;
        UpdateUI();
    }

    void UpdateUI()
    {
        string currentLang = languages[currentIndex];
        valueText.text = currentLang;

        PlayerPrefs.SetString("SavedLanguage", currentLang);
        PlayerPrefs.Save();

        // Кричим всем скриптам в игре: "Язык поменялся, обновитесь!"
        OnLanguageChanged?.Invoke();
    }
}