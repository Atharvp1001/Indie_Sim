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

        // Change to hit color (green)
        crosshairImage.color = hitColor;

        // Wait for the duration
        yield return new WaitForSeconds(hitFeedbackDuration);

        // Change back to normal color
        crosshairImage.color = normalColor;

        isShowingHitFeedback = false;
    }

    void OnDestroy()
    {
        // Show cursor again when destroyed
        Cursor.visible = true;
    }
}
