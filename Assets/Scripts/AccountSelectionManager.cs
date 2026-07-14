using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement; // Добавили пространство имен для работы со сценами

public class AccountSelectionManager : MonoBehaviour
{
    [Header("Панели")]
    public GameObject accountSelectionPanel;
    public GameObject createAccountPopup;

    [Header("Элементы списка")]
    public Transform contentParent;
    public GameObject accountRowPrefab;

    [Header("Попап создания")]
    public TMP_InputField newAccountInput;
    public Button confirmCreateButton; // Кнопка "Save" в попапе

    [Header("Основные кнопки")]
    public Button playButton;
    public Button deleteButton;

    private string selectedAccountName = "";
    private string csvPath;

    void Start()
    {
        csvPath = Path.Combine(Application.persistentDataPath, "Accounts.csv");
        accountSelectionPanel.SetActive(false);
        if (createAccountPopup != null) createAccountPopup.SetActive(false);

        // Подписываемся на событие изменения текста в инпуте
        if (newAccountInput != null)
        {
            newAccountInput.onValueChanged.AddListener(OnInputTextChanged);
        }
    }

    public void OpenSelectionScreen()
    {
        accountSelectionPanel.SetActive(true);
        // При открытии блокируем кнопки управления, пока аккаунт не выбран
        playButton.interactable = false;
        deleteButton.interactable = false;
        selectedAccountName = "";
        LoadAccountsToUI();
    }

    private void LoadAccountsToUI()
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // 1. ПЕРВАЯ ПЛАШКА "СОЗДАТЬ"
        GameObject createNewRow = Instantiate(accountRowPrefab, contentParent);
        TextMeshProUGUI[] createTexts = createNewRow.GetComponentsInChildren<TextMeshProUGUI>();

        // Записываем текст в первый компонент (Имя)
        if (createTexts.Length > 0) createTexts[0].text = "+ Создать новый аккаунт";
        // Очищаем второй компонент (Время), чтобы там не висело стандартное "New Text"
        if (createTexts.Length > 1) createTexts[1].text = "";

        createNewRow.GetComponent<Button>().onClick.AddListener(() => {
            if (createAccountPopup != null)
            {
                newAccountInput.text = "";
                // При открытии попапа кнопка Save должна быть заблокирована, так как поле ввода пустое
                if (confirmCreateButton != null) confirmCreateButton.interactable = false;
                createAccountPopup.SetActive(true);
            }
        });

        // 2. ЗАГРУЗКА ИЗ CSV
        if (!File.Exists(csvPath)) { File.WriteAllText(csvPath, "PlayerName,LastPlayed\n"); return; }

        string[] lines = File.ReadAllLines(csvPath);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] data = lines[i].Split(',');
            string name = data[0];
            string lastPlayed = data[1];

            GameObject row = Instantiate(accountRowPrefab, contentParent);
            TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();
            texts[0].text = name;

            // Выводим только саму дату, без лишних длинных фраз, чтобы ничего не вылезало
            texts[1].text = lastPlayed;

            row.GetComponent<Button>().onClick.AddListener(() => OnAccountSelected(name));
        }
    }

    private void OnAccountSelected(string accountName)
    {
        selectedAccountName = accountName;
        playButton.interactable = true;
        deleteButton.interactable = true;
    }

    // Вызывается автоматически при изменении текста в инпуте
    private void OnInputTextChanged(string text)
    {
        if (confirmCreateButton != null)
        {
            // Кнопка активна только тогда, когда в поле есть текст (исключая пробелы)
            confirmCreateButton.interactable = !string.IsNullOrWhiteSpace(text);
        }
    }

    // ВЫЗЫВАЕТСЯ КНОПКОЙ "SAVE"
    public void CreateNewAccount()
    {
        string newName = newAccountInput.text.Trim();
        if (string.IsNullOrEmpty(newName) || newName.Contains(",")) return;

        string today = System.DateTime.Now.ToString("dd/MM/yyyy");
        File.AppendAllText(csvPath, $"{newName},{today}\n");

        // Закрываем попап после сохранения
        if (createAccountPopup != null) createAccountPopup.SetActive(false);
        LoadAccountsToUI();
    }

    // ВЫЗЫВАЕТСЯ КНОПКОЙ "DELETE"
    public void DeleteSelectedAccount()
    {
        if (string.IsNullOrEmpty(selectedAccountName)) return;

        List<string> lines = File.ReadAllLines(csvPath).ToList();
        lines = lines.Where(line => !line.StartsWith(selectedAccountName + ",")).ToList();
        File.WriteAllLines(csvPath, lines);

        selectedAccountName = "";
        playButton.interactable = false;
        deleteButton.interactable = false;
        LoadAccountsToUI();
    }

    public void StartGame()
    {
        if (!string.IsNullOrEmpty(selectedAccountName))
        {
            PlayerPrefs.SetString("CurrentPlayerName", selectedAccountName);
            PlayerPrefs.Save(); // Сохраняем PlayerPrefs перед переходом
            Debug.Log($"Играем за {selectedAccountName}!");

            // Загружаем сцену с игрой
            SceneManager.LoadScene("SampleScene");
        }
    }

    // Метод для закрытия попапа (повесь на кнопку отмены или фон)
    public void CloseCreateAccountPopup() { if (createAccountPopup != null) createAccountPopup.SetActive(false); }
}