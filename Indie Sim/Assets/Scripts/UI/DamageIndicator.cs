using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DamageIndicator : MonoBehaviour
{
    [Header("Vignette Settings")]
    [SerializeField] private Volume postProcessVolume; // Assign your Post Process Volume here

    [Header("Damage Flash Settings")]
    [SerializeField] private Color damageColor = new Color(0.8f, 0f, 0f, 1f); // Red color
    [SerializeField] private Color normalColor = Color.black; // Default black
    [SerializeField] private float flashIntensity = 0.5f; // How strong the vignette becomes (0-1)
    [SerializeField] private int flashCount = 3; // Number of flashes
    [SerializeField] private float flashDuration = 0.15f; // Duration of each flash (seconds)
    [SerializeField] private float flashInterval = 0.1f; // Time between flashes (seconds)

    [Header("Normal Vignette Settings")]
    [SerializeField] private float normalIntensity = 0.3f; // Your normal vignette intensity

    private Vignette vignette;
    private bool isFlashing = false;

    private void Start()
    {
        // Get the Vignette effect from the Volume
        if (postProcessVolume == null)
        {
            Debug.LogError("[DamageIndicator] Post Process Volume is not assigned!");
            return;
        }

        if (postProcessVolume.profile.TryGet(out vignette))
        {
            // Initialize vignette to normal state
            vignette.color.value = normalColor;
            vignette.intensity.value = normalIntensity;
            Debug.Log("[DamageIndicator] Vignette successfully initialized");
        }
        else
        {
            Debug.LogError("[DamageIndicator] Vignette effect not found in Volume Profile! Add Vignette override to your Volume.");
        }
    }

    /// <summary>
    /// Call this method when the player takes damage
    /// </summary>
    public void TriggerDamageFlash()
    {
        if (vignette == null)
        {
            Debug.LogWarning("[DamageIndicator] Vignette is not set up!");
            return;
        }

        if (!isFlashing)
        {
            StartCoroutine(DamageFlashCoroutine());
        }
    }

    /// <summary>
    /// Coroutine that handles the flashing effect
    /// </summary>
    private IEnumerator DamageFlashCoroutine()
    {
        isFlashing = true;

        for (int i = 0; i < flashCount; i++)
        {
            // Flash to red
            yield return StartCoroutine(LerpVignetteColor(damageColor, flashIntensity, flashDuration));

            // Wait briefly
            yield return new WaitForSeconds(flashInterval);

            // Flash back to black (but keep intensity for a moment)
            yield return StartCoroutine(LerpVignetteColor(normalColor, flashIntensity, flashDuration));

            // Wait before next flash
            if (i < flashCount - 1) // Don't wait after the last flash
            {
                yield return new WaitForSeconds(flashInterval);
            }
        }

        // Final fade back to normal intensity
        yield return StartCoroutine(LerpVignetteIntensity(normalIntensity, flashDuration));

        isFlashing = false;
    }

    /// <summary>
    /// Smoothly change vignette color and intensity
    /// </summary>
    private IEnumerator LerpVignetteColor(Color targetColor, float targetIntensity, float duration)
    {
        Color startColor = vignette.color.value;
        float startIntensity = vignette.intensity.value;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            vignette.color.value = Color.Lerp(startColor, targetColor, t);
            vignette.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, t);

            yield return null;
        }

        vignette.color.value = targetColor;
        vignette.intensity.value = targetIntensity;
    }

    /// <summary>
    /// Smoothly change only the vignette intensity
    /// </summary>
    private IEnumerator LerpVignetteIntensity(float targetIntensity, float duration)
    {
        float startIntensity = vignette.intensity.value;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            vignette.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, t);

            yield return null;
        }

        vignette.intensity.value = targetIntensity;
    }

    /// <summary>
    /// Manual method to set vignette back to normal (if needed)
    /// </summary>
    public void ResetVignette()
    {
        if (vignette != null)
        {
            StopAllCoroutines();
            vignette.color.value = normalColor;
            vignette.intensity.value = normalIntensity;
            isFlashing = false;
        }
    }

    /// <summary>
    /// For testing - call this to preview the effect
    /// </summary>
    private void Update()
    {
        // Press 'H' to test damage flash
        if (Input.GetKeyDown(KeyCode.H))
        {
            TriggerDamageFlash();
            Debug.Log("[DamageIndicator] Test damage flash triggered!");
        }
    }
}
