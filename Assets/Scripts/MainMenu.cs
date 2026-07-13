using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI; // Обязательно добавляем для работы с компонентом Button

public class MainMenu : MonoBehaviour
{
    [Tooltip("Перетащите сюда InputField из вашего Canvas")]
    public TMP_InputField nameInputField;

    [Tooltip("Перетащите сюда объект кнопки PLAY")]
    public Button playButton;

    void Start()
    {
        if (nameInputField != null)
        {
            // Подписываемся на изменение текста: каждый раз при вводе символа будет вызываться ValidateInput
            nameInputField.onValueChanged.AddListener(ValidateInput);

            // Загружаем сохраненное имя, если оно есть
            string savedName = PlayerPrefs.GetString("CurrentPlayerName", "");
            nameInputField.text = savedName;

            // Проверяем состояние кнопки сразу при запуске сцены
            ValidateInput(savedName);
        }
    }

    // Эта функция включает или выключает кнопку
    private void ValidateInput(string input)
    {
        if (playButton != null)
        {
            // Кнопка активна только если текст не пустой и не состоит из одних пробелов
            playButton.interactable = !string.IsNullOrWhiteSpace(input);
        }
    }

    public void LoadGameScene(string sceneName)
    {
        // Сохраняем имя (теперь мы точно знаем, что оно не пустое)
        if (nameInputField != null)
        {
            string enteredName = nameInputField.text.Trim();
            PlayerPrefs.SetString("CurrentPlayerName", enteredName);
            PlayerPrefs.Save();

            Debug.Log($"<color=green>Имя перед запуском сохранено: {enteredName}</color>");
        }

        SceneManager.LoadScene(sceneName);
    }
}