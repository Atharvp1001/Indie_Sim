using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CustomCrosshair : MonoBehaviour
{
    [Header("Crosshair UI")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private RectTransform crosshairRect;
    [SerializeField] private Canvas canvas;

    [Header("Color Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hitColor = Color.green;

    [Header("Hit Feedback Settings")]
    [SerializeField] private float hitFeedbackDuration = 0.15f;

    [Header("Scale Feedback")]
    [SerializeField] private float hitScaleMultiplier = 1.3f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);

    private bool isShowingHitFeedback = false;

    void Start()
    {
        // Hide the system cursor
        Cursor.visible = false;

        // Set initial color
        if (crosshairImage != null)
        {
            crosshairImage.color = normalColor;
        }
    }

    void Update()
    {
        // Make crosshair follow mouse position
        if (crosshairRect != null && canvas != null)
        {
            Vector2 mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.worldCamera,
                out mousePosition
            );

            crosshairRect.localPosition = mousePosition;
        }
    }

    /// <summary>
    /// Call this when the player hits an enemy
    /// </summary>
    public void ShowHitFeedback()
    {
        if (!isShowingHitFeedback)
        {
            StartCoroutine(HitFeedbackCoroutine());
        }
    }

    private IEnumerator HitFeedbackCoroutine()
    {
        isShowingHitFeedback = true;

        Vector3 originalScale = crosshairRect.localScale;
        Vector3 targetScale = originalScale * hitScaleMultiplier;

        float elapsedTime = 0f;

        while (elapsedTime < hitFeedbackDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / hitFeedbackDuration;

         
             crosshairImage.color = hitColor;
            // Animate scale using curve
            float scaleValue = scaleCurve.Evaluate(t);
            crosshairRect.localScale = Vector3.Lerp(targetScale, originalScale, t);

            yield return null;
        }

        // Reset to original values
        crosshairImage.color = normalColor;
        crosshairRect.localScale = originalScale;

        isShowingHitFeedback = false;
    }


    void OnDestroy()
    {
        // Show cursor again when destroyed
        Cursor.visible = true;
    }
}
