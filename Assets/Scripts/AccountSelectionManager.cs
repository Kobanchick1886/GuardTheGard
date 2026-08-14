using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System;

public class AccountSelectionManager : MonoBehaviour
{
    [Header("Панели")]
    public GameObject accountSelectionPanel;
    public GameObject createAccountPanel;
    public GameObject optionsPanel;

    [Header("Попап удаления (Confirmation)")]
    public GameObject deleteConfirmationPanel;
    public TextMeshProUGUI deleteConfirmationText;
    public Button confirmDeleteButton;
    public Button cancelDeleteButton;
    public Button closeDeletePopupButton;

    [Header("Мануал (Обучение)")]
    public GameObject manualParentPanel; // Сюда закинешь сам объект Manual
    public GameObject manualPanelEN;
    public GameObject manualPanelUA;
    public Button openManualButton;
    public Button[] closeManualButtons;

    [Header("Элементы списка")]
    public Transform contentParent;
    public GameObject accountRowPrefab;
    public GameObject createAccountRowPrefab;

    [Header("Попап создания (Инпуты)")]
    public TMP_InputField nameInput;
    public TMP_InputField surnameInput;
    public Button confirmCreateButton;

    [Header("Поиск")]
    public TMP_InputField searchInput;

    [Header("Основные кнопки")]
    public Button playButton;
    public Button deleteButton;

    [Header("Кнопки закрытия / Назад")]
    public Button backToMenuButton;
    public Button backToPanelButton;
    public Button cancelButton;

    private string selectedAccountName = "";
    private string csvPath;

    void Start()
    {
        csvPath = Path.Combine(Application.persistentDataPath, "Accounts.csv");

        if (accountSelectionPanel != null) accountSelectionPanel.SetActive(false);
        if (createAccountPanel != null) createAccountPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (deleteConfirmationPanel != null) deleteConfirmationPanel.SetActive(false);

        // Гасим весь мануал со старта
        if (manualParentPanel != null) manualParentPanel.SetActive(false);
        if (manualPanelEN != null) manualPanelEN.SetActive(false);
        if (manualPanelUA != null) manualPanelUA.SetActive(false);

        if (nameInput != null) nameInput.onValueChanged.AddListener(OnInputTextChanged);
        if (surnameInput != null) surnameInput.onValueChanged.AddListener(OnInputTextChanged);
        if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearchInputChanged);

        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(CloseSelectionScreen);
        if (backToPanelButton != null) backToPanelButton.onClick.AddListener(CloseCreatePopup);
        if (cancelButton != null) cancelButton.onClick.AddListener(CloseCreatePopup);

        if (confirmCreateButton != null) confirmCreateButton.onClick.AddListener(CreateNewAccount);

        if (openManualButton != null)
        {
            openManualButton.onClick.RemoveAllListeners();
            openManualButton.onClick.AddListener(OpenManual);
        }

        if (closeManualButtons != null)
        {
            foreach (Button closeBtn in closeManualButtons)
            {
                if (closeBtn != null)
                {
                    closeBtn.onClick.RemoveAllListeners();
                    closeBtn.onClick.AddListener(CloseManual);
                }
            }
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OpenDeleteConfirmation);
        }

        if (confirmDeleteButton != null) confirmDeleteButton.onClick.AddListener(ConfirmAndExecuteDeletion);
        if (cancelDeleteButton != null) cancelDeleteButton.onClick.AddListener(CloseDeleteConfirmation);
        if (closeDeletePopupButton != null) closeDeletePopupButton.onClick.AddListener(CloseDeleteConfirmation);
    }

    void OnEnable()
    {
        LanguageSwitcher.OnLanguageChanged += ReloadListOnLanguageChange;
    }

    void OnDisable()
    {
        LanguageSwitcher.OnLanguageChanged -= ReloadListOnLanguageChange;
    }

    private void ReloadListOnLanguageChange()
    {
        if (accountSelectionPanel != null && accountSelectionPanel.activeInHierarchy)
        {
            string currentSearch = searchInput != null ? searchInput.text : "";
            LoadAccountsToUI(currentSearch);
        }

        bool isAnyManualOpen = (manualParentPanel != null && manualParentPanel.activeInHierarchy);

        if (isAnyManualOpen)
        {
            CloseManual();
            OpenManual();
        }

        if (deleteConfirmationPanel != null && deleteConfirmationPanel.activeInHierarchy)
        {
            UpdateDeleteConfirmationText();
        }
    }

    public void OpenDeleteConfirmation()
    {
        if (string.IsNullOrEmpty(selectedAccountName)) return;

        if (deleteConfirmationPanel != null)
        {
            deleteConfirmationPanel.SetActive(true);
            UpdateDeleteConfirmationText();
        }
    }

    public void CloseDeleteConfirmation()
    {
        if (deleteConfirmationPanel != null)
        {
            deleteConfirmationPanel.SetActive(false);
        }
    }

    private void UpdateDeleteConfirmationText()
    {
        if (deleteConfirmationText == null) return;

        string currentLang = PlayerPrefs.GetString("SavedLanguage", "English");
        string colorOrange = "#F26D50";
        string colorGreen = "#C0E15F";

        if (currentLang == "Українська")
        {
            deleteConfirmationText.text = $"Ви впевнені, що хочете <color={colorOrange}>видалити</color> наступний акаунт: <color={colorGreen}>{selectedAccountName}</color>?\nВся інформація буде втрачена!";
        }
        else
        {
            deleteConfirmationText.text = $"Are you sure you want to <color={colorOrange}>delete</color> the following account: <color={colorGreen}>{selectedAccountName}</color>?\nAll the information will be lost!";
        }
    }

    public void OpenManual()
    {
        string currentLang = PlayerPrefs.GetString("SavedLanguage", "English");

        // Включаем родителя
        if (manualParentPanel != null) manualParentPanel.SetActive(true);

        // Включаем нужный язык, выключаем ненужный
        if (currentLang == "Українська")
        {
            if (manualPanelEN != null) manualPanelEN.SetActive(false);
            if (manualPanelUA != null) manualPanelUA.SetActive(true);
        }
        else
        {
            if (manualPanelUA != null) manualPanelUA.SetActive(false);
            if (manualPanelEN != null) manualPanelEN.SetActive(true);
        }
    }

    public void CloseManual()
    {
        if (manualParentPanel != null) manualParentPanel.SetActive(false);
        if (manualPanelEN != null) manualPanelEN.SetActive(false);
        if (manualPanelUA != null) manualPanelUA.SetActive(false);
    }

    public void OpenOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void OpenSelectionScreen()
    {
        if (accountSelectionPanel != null) accountSelectionPanel.SetActive(true);
        playButton.interactable = false;
        deleteButton.interactable = false;
        selectedAccountName = "";

        if (searchInput != null) searchInput.text = "";
        LoadAccountsToUI();
    }

    public void CloseSelectionScreen()
    {
        if (accountSelectionPanel != null) accountSelectionPanel.SetActive(false);
    }

    public void OpenCreatePopup()
    {
        if (createAccountPanel != null)
        {
            nameInput.text = "";
            surnameInput.text = "";
            if (confirmCreateButton != null) confirmCreateButton.interactable = false;
            createAccountPanel.SetActive(true);
        }
    }

    public void CloseCreatePopup()
    {
        if (createAccountPanel != null) createAccountPanel.SetActive(false);
    }

    private void OnInputTextChanged(string text)
    {
        if (confirmCreateButton != null)
        {
            string combinedText = nameInput.text + surnameInput.text;
            confirmCreateButton.interactable = !string.IsNullOrWhiteSpace(combinedText);
        }
    }

    private void OnSearchInputChanged(string query)
    {
        LoadAccountsToUI(query);
    }

    public void CreateNewAccount()
    {
        string newName = nameInput.text.Trim();
        string newSurname = surnameInput.text.Trim();
        string fullName = $"{newName} {newSurname}".Trim();

        if (string.IsNullOrEmpty(fullName) || fullName.Contains(",")) return;

        string today = System.DateTime.Now.ToString("dd/MM/yyyy");
        File.AppendAllText(csvPath, $"{fullName},{today},0\n");

        CloseCreatePopup();

        if (searchInput != null) searchInput.text = "";
        LoadAccountsToUI();
    }

    private void LoadAccountsToUI(string searchQuery = "")
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        string currentLang = PlayerPrefs.GetString("SavedLanguage", "English");

        if (string.IsNullOrEmpty(searchQuery) && createAccountRowPrefab != null)
        {
            GameObject createRow = Instantiate(createAccountRowPrefab, contentParent);
            Button createBtn = createRow.GetComponent<Button>();
            if (createBtn == null) createBtn = createRow.GetComponentInChildren<Button>();
            if (createBtn != null) createBtn.onClick.AddListener(OpenCreatePopup);
        }

        if (!File.Exists(csvPath)) { File.WriteAllText(csvPath, "PlayerName,CreationDate,BestTime\n"); return; }

        string[] lines = File.ReadAllLines(csvPath);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] data = lines[i].Split(',');
            string name = data[0];
            string creationDate = data.Length > 1 ? data[1] : "";

            float bestTimeSeconds = 0f;
            if (data.Length > 2)
            {
                float.TryParse(data[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out bestTimeSeconds);
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                if (!name.ToLower().Contains(searchQuery.ToLower())) continue;
            }

            GameObject row = Instantiate(accountRowPrefab, contentParent);

            TextMeshProUGUI nameText = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI timeText = row.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI bestTimeText = row.transform.Find("BestTime")?.GetComponent<TextMeshProUGUI>();

            if (nameText != null) nameText.text = name;

            string formattedTime = bestTimeSeconds > 0f ? FormatTime(bestTimeSeconds) : "--:--";

            if (currentLang == "Українська")
            {
                if (bestTimeText != null) bestTimeText.text = $"Кращий час: {formattedTime}";
                if (timeText != null) timeText.text = $"Створено: {creationDate}";
            }
            else
            {
                if (bestTimeText != null) bestTimeText.text = $"Best time: {formattedTime}";
                if (timeText != null) timeText.text = $"Created: {creationDate}";
            }

            row.GetComponent<Button>().onClick.AddListener(() => OnAccountSelected(name));
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(timeInSeconds);
        return time.ToString(@"hh\:mm\:ss");
    }

    public void QuitGame()
    {
        Debug.Log("Выход из игры...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnAccountSelected(string accountName)
    {
        selectedAccountName = accountName;
        playButton.interactable = true;
        deleteButton.interactable = true;
    }

    public void ConfirmAndExecuteDeletion()
    {
        if (string.IsNullOrEmpty(selectedAccountName)) return;

        // 1. Удаляем из Accounts.csv
        List<string> lines = File.ReadAllLines(csvPath).ToList();
        lines = lines.Where(line => !line.StartsWith(selectedAccountName + ",")).ToList();
        File.WriteAllLines(csvPath, lines);

        // 2. Удаляем из DefeatAnalytics.csv
        string analyticsPath = Path.Combine(Application.persistentDataPath, "DefeatAnalytics.csv");
        if (File.Exists(analyticsPath))
        {
            List<string> analyticsLines = File.ReadAllLines(analyticsPath).ToList();

            // Оставляем нулевую строку (заголовки) и те строки, где имя игрока (3-я колонка, индекс 2) не совпадает с удаляемым
            analyticsLines = analyticsLines.Where((line, index) =>
            {
                if (index == 0) return true;

                string[] parts = line.Split(',');
                if (parts.Length > 2)
                {
                    return parts[2] != selectedAccountName;
                }
                return true;
            }).ToList();

            File.WriteAllLines(analyticsPath, analyticsLines);
        }

        // 3. Удаляем счетчик сессий из памяти
        PlayerPrefs.DeleteKey("SessionCount_" + selectedAccountName);
        PlayerPrefs.Save();

        // 4. Обновляем интерфейс
        selectedAccountName = "";
        playButton.interactable = false;
        deleteButton.interactable = false;

        CloseDeleteConfirmation();

        string currentSearch = searchInput != null ? searchInput.text : "";
        LoadAccountsToUI(currentSearch);
    }

    public void StartGame()
    {
        if (!string.IsNullOrEmpty(selectedAccountName))
        {
            PlayerPrefs.SetString("CurrentPlayerName", selectedAccountName);
            PlayerPrefs.Save();
            Debug.Log($"Граємо за {selectedAccountName}!");
            SceneManager.LoadScene("SampleScene");
        }
    }
}