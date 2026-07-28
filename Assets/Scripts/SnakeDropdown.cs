using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SnakeDropdown : MonoBehaviour
{
    [Header("UI Elements")]
    public Transform middleContainer;
    public RectTransform middleRect;
    public TextMeshProUGUI mainButtonText;
    public RectTransform mainTextRect;

    [Header("Animation Settings")]
    public GameObject itemPrefab;
    public float dropdownAnimSpeed = 15f;
    public float itemHeight = 50f;

    [Header("Text Movement & Scale")]
    public float textClosedY = -25f;
    public float textOpenedY = 0f;
    public float textClosedSize = 30f;
    public float textOpenedSize = 24f;
    public float textAnimSpeed = 15f;

    private bool isOpen = false;
    private Coroutine animCoroutine;
    private RectTransform myRectTransform;

    private List<string> resolutions = new List<string> {
        "3840x2160",
        "2560x1440",
        "1920x1080",
        "1600x1050",
        "1600x900",
        "1280x800",
        "1280x720"
    };

    void Start()
    {
        myRectTransform = GetComponent<RectTransform>();

        mainButtonText.text = "1920x1080";
        mainTextRect.anchoredPosition = new Vector2(mainTextRect.anchoredPosition.x, textClosedY);
        mainButtonText.fontSize = textClosedSize;

        middleRect.sizeDelta = new Vector2(middleRect.sizeDelta.x, 0);
    }

    public void ToggleDropdown()
    {
        isOpen = !isOpen;

        if (animCoroutine != null) StopCoroutine(animCoroutine);

        if (isOpen)
        {
            SpawnAllItems();
            float targetHeight = resolutions.Count * itemHeight;
            animCoroutine = StartCoroutine(AnimateDropdown(targetHeight, textOpenedY, textOpenedSize));
        }
        else
        {
            animCoroutine = StartCoroutine(AnimateDropdown(0f, textClosedY, textClosedSize, true));
        }
    }

    public void SelectResolution(string selectedRes)
    {
        mainButtonText.text = selectedRes;

        if (selectedRes.Contains("3840")) Screen.SetResolution(3840, 2160, FullScreenMode.FullScreenWindow);
        else if (selectedRes.Contains("2560")) Screen.SetResolution(2560, 1440, FullScreenMode.FullScreenWindow);
        else if (selectedRes.Contains("1920")) Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
        else if (selectedRes.Contains("1600x1050")) Screen.SetResolution(1600, 1050, FullScreenMode.FullScreenWindow);
        else if (selectedRes.Contains("1600x900")) Screen.SetResolution(1600, 900, FullScreenMode.FullScreenWindow);
        else if (selectedRes.Contains("1280x800")) Screen.SetResolution(1280, 800, FullScreenMode.FullScreenWindow);
        else if (selectedRes.Contains("1280x720")) Screen.SetResolution(1280, 720, FullScreenMode.FullScreenWindow);

        isOpen = false;

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateDropdown(0f, textClosedY, textClosedSize, true));
    }

    IEnumerator AnimateDropdown(float targetHeight, float targetTextY, float targetTextSize, bool closeAfter = false)
    {
        while (Mathf.Abs(middleRect.sizeDelta.y - targetHeight) > 0.5f ||
               Mathf.Abs(mainButtonText.fontSize - targetTextSize) > 0.1f)
        {
            float newHeight = Mathf.Lerp(middleRect.sizeDelta.y, targetHeight, Time.deltaTime * dropdownAnimSpeed);
            middleRect.sizeDelta = new Vector2(middleRect.sizeDelta.x, newHeight);

            Vector2 targetPos = new Vector2(mainTextRect.anchoredPosition.x, targetTextY);
            mainTextRect.anchoredPosition = Vector2.Lerp(mainTextRect.anchoredPosition, targetPos, Time.deltaTime * textAnimSpeed);
            mainButtonText.fontSize = Mathf.Lerp(mainButtonText.fontSize, targetTextSize, Time.deltaTime * textAnimSpeed);

            LayoutRebuilder.ForceRebuildLayoutImmediate(myRectTransform);

            yield return null;
        }

        middleRect.sizeDelta = new Vector2(middleRect.sizeDelta.x, targetHeight);
        mainTextRect.anchoredPosition = new Vector2(mainTextRect.anchoredPosition.x, targetTextY);
        mainButtonText.fontSize = targetTextSize;

        LayoutRebuilder.ForceRebuildLayoutImmediate(myRectTransform);

        if (closeAfter)
        {
            ClearItems();
        }
    }

    void SpawnAllItems()
    {
        ClearItems();
        foreach (string res in resolutions)
        {
            GameObject newItem = Instantiate(itemPrefab, middleContainer);
            newItem.GetComponentInChildren<TextMeshProUGUI>().text = res;

            Button itemButton = newItem.GetComponent<Button>();

            // Чистим старые связи с префаба, чтобы они не крашили событие
            itemButton.onClick.RemoveAllListeners();

            // Жестко фиксируем текущее значение в новую переменную
            string capturedRes = res;

            itemButton.onClick.AddListener(() => SelectResolution(capturedRes));
        }
    }

    void ClearItems()
    {
        foreach (Transform child in middleContainer)
        {
            Destroy(child.gameObject);
        }
    }
}