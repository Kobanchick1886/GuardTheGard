using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;

public class AccountSelectionManager : MonoBehaviour
{
    [Header("Панели")]
    public GameObject accountSelectionPanel;
    public GameObject createAccountPanel;
    public GameObject optionsPanel;

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

        if (nameInput != null) nameInput.onValueChanged.AddListener(OnInputTextChanged);
        if (surnameInput != null) surnameInput.onValueChanged.AddListener(OnInputTextChanged);

        if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearchInputChanged);

        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(CloseSelectionScreen);
        if (backToPanelButton != null) backToPanelButton.onClick.AddListener(CloseCreatePopup);
        if (cancelButton != null) cancelButton.onClick.AddListener(CloseCreatePopup);

        if (confirmCreateButton != null) confirmCreateButton.onClick.AddListener(CreateNewAccount);
    }

    // --- ПОДПИСКА НА ИЗМЕНЕНИЕ ЯЗЫКА ДЛЯ МГНОВЕННОГО ПЕРЕВОДА СПИСКА ---
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
        // Перезагружаем список только если панель выбора сейчас открыта
        if (accountSelectionPanel != null && accountSelectionPanel.activeInHierarchy)
        {
            string currentSearch = searchInput != null ? searchInput.text : "";
            LoadAccountsToUI(currentSearch);
        }
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
        File.AppendAllText(csvPath, $"{fullName},{today}\n");

        CloseCreatePopup();

        if (searchInput != null) searchInput.text = "";
        LoadAccountsToUI();
    }

    private void LoadAccountsToUI(string searchQuery = "")
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // Читаем текущий язык один раз перед циклом
        string currentLang = PlayerPrefs.GetString("SavedLanguage", "English");

        // 1. СОЗДАЕМ КНОПКУ "СОЗДАТЬ АККАУНТ"
        if (string.IsNullOrEmpty(searchQuery) && createAccountRowPrefab != null)
        {
            GameObject createRow = Instantiate(createAccountRowPrefab, contentParent);

            Button createBtn = createRow.GetComponent<Button>();
            if (createBtn == null) createBtn = createRow.GetComponentInChildren<Button>();

            if (createBtn != null)
            {
                createBtn.onClick.AddListener(OpenCreatePopup);
            }
        }

        // 2. ЗАГРУЖАЕМ ОСТАЛЬНЫЕ АККАУНТЫ
        if (!File.Exists(csvPath)) { File.WriteAllText(csvPath, "PlayerName,LastPlayed\n"); return; }

        string[] lines = File.ReadAllLines(csvPath);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] data = lines[i].Split(',');
            string name = data[0];
            string lastPlayed = data.Length > 1 ? data[1] : "";

            if (!string.IsNullOrEmpty(searchQuery))
            {
                if (!name.ToLower().Contains(searchQuery.ToLower())) continue;
            }

            GameObject row = Instantiate(accountRowPrefab, contentParent);

            TextMeshProUGUI nameText = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI timeText = row.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI bestTimeText = row.transform.Find("BestTime")?.GetComponent<TextMeshProUGUI>();

            if (nameText != null) nameText.text = name;

            // --- ПЕРЕВОД ДИНАМИЧЕСКОГО ТЕКСТА ---
            if (currentLang == "Українська")
            {
                if (bestTimeText != null) bestTimeText.text = "Кращий час: --:--";
                if (timeText != null) timeText.text = $"Остання дата гри: {lastPlayed}";
            }
            else // English
            {
                if (bestTimeText != null) bestTimeText.text = "Best time: --:--";
                if (timeText != null) timeText.text = $"Last time played: {lastPlayed}";
            }

            row.GetComponent<Button>().onClick.AddListener(() => OnAccountSelected(name));
        }
    }
    public void QuitGame()
    {
        Debug.Log("Выход из игры...");
        Application.Quit(); // Закрывает сбилженную игру

        // Если ты тестируешь прямо в редакторе Unity, Application.Quit() не закроет редактор. 
        // Эта строчка остановит режим Play в редакторе (можешь оставить её для удобства):
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

    public void DeleteSelectedAccount()
    {
        if (string.IsNullOrEmpty(selectedAccountName)) return;

        List<string> lines = File.ReadAllLines(csvPath).ToList();
        lines = lines.Where(line => !line.StartsWith(selectedAccountName + ",")).ToList();
        File.WriteAllLines(csvPath, lines);

        selectedAccountName = "";
        playButton.interactable = false;
        deleteButton.interactable = false;

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