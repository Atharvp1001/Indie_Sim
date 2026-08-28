using System.Collections;
using UnityEngine;

public class EscapePentagram : MonoBehaviour
{
    [Header("Escape Pentagram Settings")]
    [SerializeField] private float radius = 2f;
    [SerializeField] private float warningDuration = 1.5f;
    [SerializeField] private int damage = 20;

    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer pentagramRenderer;
    [SerializeField] private Color warningColor = new Color(1f, 1f, 0f, 1f); // Yellow
    [SerializeField] private Color activeColor = new Color(1f, 0f, 0f, 1f);   // Red
    [SerializeField] private Color glowColor = new Color(1f, 0.5f, 0f, 1f);   // Orange glow

    [Header("Animation Settings")]
    [SerializeField] private float startScale = 3f; // Starting scale multiplier
    [SerializeField] private AnimationCurve scaleInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float glowPulseSpeed = 2f;
    [SerializeField] private float glowIntensity = 0.3f;

    [Header("References")]
    private CircleCollider2D triggerCollider;
    private Transform player;
    private PlayerHealth playerHealth; // NEW: Reference to PlayerHealth

    private bool isWarningPhase = true;
    private bool isActive = false;
    private Vector3 originalScale;

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        // Setup collider
        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        triggerCollider.isTrigger = true;
        triggerCollider.radius = radius;

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>(); // NEW: Get PlayerHealth component
            
            if (playerHealth == null)
            {
                Debug.LogWarning("[EscapePentagram] PlayerHealth component not found on player!");
            }
        }

        // Store original scale and set to warning color
        if (pentagramRenderer != null)
        {
            originalScale = pentagramRenderer.transform.localScale;
            pentagramRenderer.transform.localScale = originalScale * startScale;
            pentagramRenderer.color = warningColor;
        }

        // Start sequence
        StartCoroutine(EscapePentagramSequence());
    }

    private IEnumerator EscapePentagramSequence()
    {
        // Warning phase with scale in
        isWarningPhase = true;
        Debug.Log("[EscapePentagram] Warning phase started");

        // Scale in during warning duration
        yield return StartCoroutine(ScaleInPentagram(warningDuration));

        // Activate
        isWarningPhase = false;
        isActive = true;
        Debug.Log("[EscapePentagram] Activated! Checking for player...");

        // Start glow effect
        StartCoroutine(GlowPulse());

        // Check if player is inside
        if (IsPlayerInside())
        {
            DamagePlayer();
        }

        // Destroy after brief display
        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }

    private IEnumerator ScaleInPentagram(float duration)
    {
        if (pentagramRenderer == null) yield break;

        float elapsed = 0f;
        Vector3 startScaleVector = originalScale * startScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Use animation curve for smooth scale
            float curveValue = scaleInCurve.Evaluate(t);
            
            Vector3 newScale = Vector3.Lerp(startScaleVector, originalScale, curveValue);
            pentagramRenderer.transform.localScale = newScale;

            yield return null;
        }

        // Ensure we end at original scale
        pentagramRenderer.transform.localScale = originalScale;
    }

    private IEnumerator GlowPulse()
    {
        if (pentagramRenderer == null) yield break;

        while (isActive)
        {
            float pulse = Mathf.Sin(Time.time * glowPulseSpeed) * glowIntensity;
            pulse = (pulse + 1f) / 2f; // Normalize to 0-1 range

            // Blend between active color and glow color
            Color glowedColor = Color.Lerp(activeColor, glowColor, pulse);
            pentagramRenderer.color = glowedColor;

            yield return null;
        }
    }

    private bool IsPlayerInside()
    {
        if (player == null) return false;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        return distanceToPlayer <= radius;
    }

    private void DamagePlayer()
    {
        if (player == null || playerHealth == null) return;

        // Use PlayerHealth's TakeDamage method with pentagram's position for knockback
        playerHealth.TakeDamage(damage, transform.position);
        Debug.Log($"[EscapePentagram] Damaged player for {damage}");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isWarningPhase ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}