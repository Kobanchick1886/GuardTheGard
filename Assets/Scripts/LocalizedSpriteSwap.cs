using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class LocalizedSpriteSwap : MonoBehaviour
{
    [Header("Английская версия")]
    public Sprite normalEN;
    public Sprite highlightedEN;
    public Sprite pressedEN;

    [Header("Украинская версия")]
    public Sprite normalUA;
    public Sprite highlightedUA;
    public Sprite pressedUA;

    private Button btn;
    private Image img;

    void Awake()
    {
        btn = GetComponent<Button>();
        img = GetComponent<Image>();
    }

    void Start()
    {
        UpdateLanguage();
    }

    void OnEnable()
    {
        LanguageSwitcher.OnLanguageChanged += UpdateLanguage;
        UpdateLanguage(); // <--- ДОБАВЛЕНО: Обновляем кнопку сразу при включении
    }

    void OnDisable()
    {
        LanguageSwitcher.OnLanguageChanged -= UpdateLanguage;
    }

    public void UpdateLanguage()
    {
        string currentLang = PlayerPrefs.GetString("SavedLanguage", "English");

        // Достаем текущие состояния кнопки
        SpriteState state = btn.spriteState;

        if (currentLang == "Українська")
        {
            if (normalUA != null) img.sprite = normalUA;
            if (highlightedUA != null) state.highlightedSprite = highlightedUA;
            if (pressedUA != null) state.pressedSprite = pressedUA;
        }
        else // English
        {
            if (normalEN != null) img.sprite = normalEN;
            if (highlightedEN != null) state.highlightedSprite = highlightedEN;
            if (pressedEN != null) state.pressedSprite = pressedEN;
        }

        // Возвращаем обновленные состояния обратно в кнопку
        btn.spriteState = state;
    }
}