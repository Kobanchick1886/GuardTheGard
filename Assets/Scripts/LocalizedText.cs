using UnityEngine;
using TMPro; // Обязательно для работы с TextMeshPro

[RequireComponent(typeof(TextMeshProUGUI))] // Требуем компонент текста
public class LocalizedText : MonoBehaviour
{
    [Header("Текст для разных языков")]
    [TextArea(2, 5)] // Делает поле ввода в инспекторе шире и удобнее
    public string englishText;

    [TextArea(2, 5)]
    public string ukrainianText;

    private TextMeshProUGUI textComponent;

    void Awake()
    {
        // Находим компонент текста на этом же объекте
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        // При старте проверяем, какой язык стоит
        UpdateLanguage();
    }

    // Подписываемся на оповещения от LanguageSwitcher
    void OnEnable()
    {
        LanguageSwitcher.OnLanguageChanged += UpdateLanguage;
        UpdateLanguage(); // <--- ДОБАВЛЕНО: Обновляем текст сразу при включении объекта
    }

    // Отписываемся при выключении
    void OnDisable()
    {
        LanguageSwitcher.OnLanguageChanged -= UpdateLanguage;
    }

    // Метод, который меняет сам текст
    public void UpdateLanguage()
    {
        string currentLang = PlayerPrefs.GetString("SavedLanguage", "English");

        if (currentLang == "Українська")
        {
            textComponent.text = ukrainianText;
        }
        else // Для English
        {
            textComponent.text = englishText;
        }
    }
}