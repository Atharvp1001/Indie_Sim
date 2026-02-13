using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CustomCrosshair : MonoBehaviour
{
    [Header("Crosshair UI")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private RectTransform crosshairRect;
    [SerializeField] private Canvas canvas;

    [Header("Hitmarker")]
    [SerializeField] private GameObject hitmarkerObject; // The child hitmarker GameObject
    [SerializeField] private float hitmarkerDuration = 0.15f;

    [Header("Scale Feedback")]
    [SerializeField] private bool enableScaleFeedback = true;
    [SerializeField] private float hitScaleMultiplier = 1.3f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);

    private bool isShowingHitFeedback = false;

    void Start()
    {
        // Hide the system cursor
        Cursor.visible = false;

        // Make sure hitmarker starts disabled
        if (hitmarkerObject != null)
        {
            hitmarkerObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("CustomCrosshair: Hitmarker GameObject not assigned! Drag the hitmarker child object into the Inspector.");
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

        // Enable the hitmarker
        if (hitmarkerObject != null)
        {
            hitmarkerObject.SetActive(true);
        }

        // Optional scale feedback
        Vector3 originalScale = crosshairRect.localScale;
        Vector3 targetScale = originalScale * hitScaleMultiplier;

        float elapsedTime = 0f;

        while (elapsedTime < hitmarkerDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / hitmarkerDuration;

            // Animate scale using curve (if enabled)
            if (enableScaleFeedback)
            {
                float scaleValue = scaleCurve.Evaluate(t);
                crosshairRect.localScale = Vector3.Lerp(targetScale, originalScale, t);
            }

            yield return null;
        }

        // Hide the hitmarker
        if (hitmarkerObject != null)
        {
            hitmarkerObject.SetActive(false);
        }

        // Reset scale to original
        if (enableScaleFeedback)
        {
            crosshairRect.localScale = originalScale;
        }

        isShowingHitFeedback = false;
    }

    void OnDestroy()
    {
        // Show cursor again when destroyed
        Cursor.visible = true;
    }
}
