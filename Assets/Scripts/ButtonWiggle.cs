using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonWiggleCurve : MonoBehaviour, IPointerEnterHandler
{
    [Header("Animation settings")]
    [Tooltip("Curve")]
    public AnimationCurve wiggleCurve = new AnimationCurve();

    [Tooltip("Max angle")]
    public float maxAngle = 15f;

    [Tooltip("Duration")]
    public float duration = 0.6f;

    private Coroutine currentCoroutine;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(AnimateWiggle());
    }

    private IEnumerator AnimateWiggle()
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            float currentAngle = wiggleCurve.Evaluate(progress) * maxAngle;

            transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);

            yield return null;
        }

        transform.localRotation = Quaternion.identity;
    }
}