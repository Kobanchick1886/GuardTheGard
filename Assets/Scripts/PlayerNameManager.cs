using UnityEngine;
using TMPro; // Обязательно, если используете TextMeshPro для UI

public class PlayerNameManager : MonoBehaviour
{
    [Tooltip("Перетащите сюда InputField из UI")]
    public TMP_InputField nameInputField;

    private void Start()
    {
        // Подгружаем имя в поле ввода, если игрок уже вводил его при прошлых запусках
        if (nameInputField != null)
        {
            nameInputField.text = PlayerPrefs.GetString("CurrentPlayerName", "");
        }
    }

    // Эту функцию нужно повесить на кнопку "Start Game" или "Save Name"
    public void SavePlayerName()
    {
        string enteredName = nameInputField.text.Trim();

        if (!string.IsNullOrEmpty(enteredName))
        {
            PlayerPrefs.SetString("CurrentPlayerName", enteredName);
            PlayerPrefs.Save();
            Debug.Log("Имя игрока сохранено: " + enteredName);
        }
        else
        {
            Debug.LogWarning("Имя не введено! Статистика запишется под именем Unknown.");
        }
    }
}