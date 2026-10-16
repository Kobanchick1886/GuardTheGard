using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

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
    public GameObject manualParentPanel;
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

    [Header("Сцены (перетащите сюда файлы сцен)")]
#if UNITY_EDITOR
    public SceneAsset singleplayerScene;
    public SceneAsset multiplayerScene;
#endif
    [HideInInspector] public string singleplayerSceneName = "SampleScene";
    [HideInInspector] public string multiplayerSceneName = "SampleSceneMultiplayer";

    // Хранит от 0 до 2 выбранных игроков
    private List<string> selectedAccounts = new List<string>();
    private string csvPath;

    // Ссылки на текстовые поля строк для отрисовки меток [P1] и [P2]
    private Dictionary<string, TextMeshProUGUI> spawnedRowTexts = new Dictionary<string, TextMeshProUGUI>();

#if UNITY_EDITOR
    // Автоматически сохраняет имена перетащенных сцен при изменении в Инспекторе
    private void OnValidate()
    {
        if (singleplayerScene != null)
            singleplayerSceneName = singleplayerScene.name;

        if (multiplayerScene != null)
            multiplayerSceneName = multiplayerScene.name;
    }
#endif

    void Start()
    {
        csvPath = Path.Combine(Application.persistentDataPath, "Accounts.csv");

        if (accountSelectionPanel != null) accountSelectionPanel.SetActive(false);
        if (createAccountPanel != null) createAccountPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (deleteConfirmationPanel != null) deleteConfirmationPanel.SetActive(false);

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

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(StartGame);
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
        if (selectedAccounts.Count != 1) return;

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
        if (deleteConfirmationText == null || selectedAccounts.Count == 0) return;

        string targetAccount = selectedAccounts[0];
        string currentLang = PlayerPrefs.GetString("SavedLanguage", "English");
        string colorOrange = "#F26D50";
        string colorGreen = "#C0E15F";

        if (currentLang == "Українська")
        {
            deleteConfirmationText.text = string.Format(
                "Ви впевнені, що хочете <color={0}>видалити</color> наступний акаунт: <color={1}>{2}</color>?\nВся інформація буде втрачена!",
                colorOrange, colorGreen, targetAccount);
        }
        else
        {
            deleteConfirmationText.text = string.Format(
                "Are you sure you want to <color={0}>delete</color> the following account: <color={1}>{2}</color>?\nAll the information will be lost!",
                colorOrange, colorGreen, targetAccount);
        }
    }

    public void OpenManual()
    {
        string currentLang = PlayerPrefs.GetString("SavedLanguage", "English");

        if (manualParentPanel != null) manualParentPanel.SetActive(true);

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
        selectedAccounts.Clear();
        UpdateActionButtons();

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
        string fullName = (newName + " " + newSurname).Trim();

        if (string.IsNullOrEmpty(fullName) || fullName.Contains(",")) return;

        string today = System.DateTime.Now.ToString("dd/MM/yyyy");
        File.AppendAllText(csvPath, fullName + "," + today + ",0\n");

        CloseCreatePopup();

        if (searchInput != null) searchInput.text = "";
        LoadAccountsToUI();
    }

    private void LoadAccountsToUI(string searchQuery = "")
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);
        spawnedRowTexts.Clear();

        string currentLang = PlayerPrefs.GetString("SavedLanguage", "English");

        if (string.IsNullOrEmpty(searchQuery) && createAccountRowPrefab != null)
        {
            GameObject createRow = Instantiate(createAccountRowPrefab, contentParent);
            Button createBtn = createRow.GetComponent<Button>();
            if (createBtn == null) createBtn = createRow.GetComponentInChildren<Button>();
            if (createBtn != null) createBtn.onClick.AddListener(OpenCreatePopup);
        }

        if (!File.Exists(csvPath))
        {
            File.WriteAllText(csvPath, "PlayerName,CreationDate,BestTime\n");
            return;
        }

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

            if (nameText != null)
            {
                spawnedRowTexts[name] = nameText;
            }

            string formattedTime = bestTimeSeconds > 0f ? FormatTime(bestTimeSeconds) : "--:--";

            if (currentLang == "Українська")
            {
                if (bestTimeText != null) bestTimeText.text = "Кращий час: " + formattedTime;
                if (timeText != null) timeText.text = "Створено: " + creationDate;
            }
            else
            {
                if (bestTimeText != null) bestTimeText.text = "Best time: " + formattedTime;
                if (timeText != null) timeText.text = "Created: " + creationDate;
            }

            string capturedName = name;
            row.GetComponent<Button>().onClick.AddListener(() => OnAccountSelected(capturedName));
        }

        RefreshSelectionLabels();
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
        if (selectedAccounts.Contains(accountName))
        {
            selectedAccounts.Remove(accountName);
        }
        else
        {
            if (selectedAccounts.Count < 2)
            {
                selectedAccounts.Add(accountName);
            }
            else
            {
                selectedAccounts[1] = accountName;
            }
        }

        UpdateActionButtons();
        RefreshSelectionLabels();
    }

    private void UpdateActionButtons()
    {
        playButton.interactable = (selectedAccounts.Count == 1 || selectedAccounts.Count == 2);
        deleteButton.interactable = (selectedAccounts.Count == 1);
    }

    private void RefreshSelectionLabels()
    {
        foreach (var kvp in spawnedRowTexts)
        {
            string accName = kvp.Key;
            TextMeshProUGUI label = kvp.Value;
            if (label == null) continue;

            int index = selectedAccounts.IndexOf(accName);
            if (index == 0)
                label.text = "<color=#C0E15F>[P1]</color> " + accName;
            else if (index == 1)
                label.text = "<color=#50C8F2>[P2]</color> " + accName;
            else
                label.text = accName;
        }
    }

    public void ConfirmAndExecuteDeletion()
    {
        if (selectedAccounts.Count != 1) return;
        string accountToDelete = selectedAccounts[0];

        // 1. Удаляем из Accounts.csv
        if (File.Exists(csvPath))
        {
            List<string> lines = File.ReadAllLines(csvPath).ToList();
            lines = lines.Where(line => !line.StartsWith(accountToDelete + ",")).ToList();
            File.WriteAllLines(csvPath, lines);
        }

        // 2. Удаляем из DefeatAnalytics.csv (одиночная)
        string analyticsPath = Path.Combine(Application.persistentDataPath, "DefeatAnalytics.csv");
        if (File.Exists(analyticsPath))
        {
            List<string> analyticsLines = File.ReadAllLines(analyticsPath).ToList();
            analyticsLines = analyticsLines.Where((line, index) =>
            {
                if (index == 0) return true;
                string[] parts = line.Split(',');
                if (parts.Length > 2) return parts[2] != accountToDelete;
                return true;
            }).ToList();

            File.WriteAllLines(analyticsPath, analyticsLines);
        }

        // 3. Удаляем из DefeatAnalyticsMultiplayer.csv (сплит-скрин)
        string multiAnalyticsPath = Path.Combine(Application.persistentDataPath, "DefeatAnalyticsMultiplayer.csv");
        if (File.Exists(multiAnalyticsPath))
        {
            List<string> multiLines = File.ReadAllLines(multiAnalyticsPath).ToList();
            multiLines = multiLines.Where((line, index) =>
            {
                if (index == 0) return true;
                string[] parts = line.Split(',');
                if (parts.Length > 3) return parts[2] != accountToDelete && parts[3] != accountToDelete;
                return true;
            }).ToList();

            File.WriteAllLines(multiAnalyticsPath, multiLines);
        }

        // 4. Удаляем счетчик сессий из памяти
        PlayerPrefs.DeleteKey("SessionCount_" + accountToDelete);
        PlayerPrefs.Save();

        // 5. Обновляем интерфейс
        selectedAccounts.Clear();
        UpdateActionButtons();

        CloseDeleteConfirmation();

        string currentSearch = searchInput != null ? searchInput.text : "";
        LoadAccountsToUI(currentSearch);
    }

    public void StartGame()
    {
        if (selectedAccounts.Count == 1)
        {
            PlayerPrefs.SetInt("IsMultiplayer", 0);
            PlayerPrefs.SetString("CurrentPlayerName", selectedAccounts[0]);
            PlayerPrefs.Save();

            Debug.Log("Граємо в соло за " + selectedAccounts[0] + "!");
            LoadTargetScene(singleplayerSceneName, true);
        }
        else if (selectedAccounts.Count == 2)
        {
            PlayerPrefs.SetInt("IsMultiplayer", 1);
            PlayerPrefs.SetString("Player1Name", selectedAccounts[0]);
            PlayerPrefs.SetString("Player2Name", selectedAccounts[1]);
            PlayerPrefs.Save();

            Debug.Log("Граємо в спліт-скрін: P1 = " + selectedAccounts[0] + ", P2 = " + selectedAccounts[1] + "!");
            LoadTargetScene(multiplayerSceneName, false);
        }
    }

    private void LoadTargetScene(string fallbackSceneName, bool isSingleplayer)
    {
#if UNITY_EDITOR
        SceneAsset targetAsset = isSingleplayer ? singleplayerScene : multiplayerScene;
        if (targetAsset != null)
        {
            string scenePath = AssetDatabase.GetAssetPath(targetAsset);
            EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(fallbackSceneName);
    }
}