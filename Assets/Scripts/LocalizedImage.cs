using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))] // Требуем именно RawImage
public class LocalizedImage : MonoBehaviour
{
    [Header("Картинки для разных языков (Текстуры)")]
    public Texture englishTexture;   // Sprite заменили на Texture
    public Texture ukrainianTexture; // Sprite заменили на Texture

    private RawImage rawImg; // Image заменили на RawImage

    void Awake()
    {
        // Находим компонент RawImage на этом же объекте
        rawImg = GetComponent<RawImage>();
    }

    void Start()
    {
        // При старте проверяем, какой язык стоит
        UpdateLanguage();
    }

    // Когда объект включается, он подписывается на оповещения от LanguageSwitcher
    void OnEnable()
    {
        LanguageSwitcher.OnLanguageChanged += UpdateLanguage;
        UpdateLanguage(); // <--- ДОБАВЛЕНО: Обновляем картинку сразу при включении
    }

    // Когда выключается — отписывается, чтобы не было ошибок
    void OnDisable()
    {
        LanguageSwitcher.OnLanguageChanged -= UpdateLanguage;
    }

    // Метод, который меняет саму картинку
    public void UpdateLanguage()
    {
        string currentLang = PlayerPrefs.GetString("SavedLanguage", "English");

        if (currentLang == "Українська")
        {
            if (ukrainianTexture != null)
            {
                rawImg.texture = ukrainianTexture;
                rawImg.SetNativeSize(); // <--- Подгоняет размер под укр. картинку
            }
        }
        else // Для English
        {
            if (englishTexture != null)
            {
                rawImg.texture = englishTexture;
                rawImg.SetNativeSize(); // <--- Подгоняет размер под англ. картинку
            }
        }
    }
}