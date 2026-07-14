using UnityEngine;

public class BackgroundShifter : MonoBehaviour
{
    [Header("Настройки движения")]
    [Tooltip("Как далеко фон уезжает влево и вправо")]
    public float amplitude = 50f;

    [Tooltip("Скорость покачивания")]
    public float speed = 0.5f;

    private RectTransform rectTransform;
    private Vector2 startPosition;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        float shiftX = Mathf.Sin(Time.time * speed) * amplitude;
        rectTransform.anchoredPosition = new Vector2(startPosition.x + shiftX, startPosition.y);
    }
}