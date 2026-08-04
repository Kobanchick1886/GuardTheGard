using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LanguageSwitcher : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI valueText; // Текст по центру (например, "English")
    public Button leftButton;         // Кнопка влево
    public Button rightButton;        // Кнопка вправо

    [Header("Settings")]
    private List<string> languages = new List<string> { "English", "Українська" }; // Можешь добавить еще языки
    private int currentIndex = 0;

    void Start()
    {
        leftButton.onClick.AddListener(PrevLanguage);
        rightButton.onClick.AddListener(NextLanguage);
        string savedLang = PlayerPrefs.GetString("SavedLanguage", "English");
        int savedIndex = languages.IndexOf(savedLang);
        if (savedIndex != -1)
        {
            currentIndex = savedIndex;
        }
        else
        {
            currentIndex = 0;
        }
        UpdateUI();
    }

    public void NextLanguage()
    {
        currentIndex++;
        if (currentIndex >= languages.Count)
        {
            currentIndex = 0;
        }
        UpdateUI();
    }

    public void PrevLanguage()
    {
        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = languages.Count - 1; 
        }
        UpdateUI();
    }

    void UpdateUI()
    {
        string currentLang = languages[currentIndex];
        valueText.text = currentLang;
        PlayerPrefs.SetString("SavedLanguage", currentLang);
        PlayerPrefs.Save();
        Debug.Log("Выбран и сохранен язык: " + currentLang);
    }
}