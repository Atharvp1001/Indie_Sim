using System.Collections;
using UnityEngine;

public class PrisonPentagram : MonoBehaviour
{
    [Header("Prison Pentagram Settings")]
    [SerializeField] private float radius = 2f;
    [SerializeField] private float warningDuration = 0.8f;
    [SerializeField] private float trapDuration = 3f;

    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer pentagramRenderer;
    [SerializeField] private Color warningColor = new Color(0.5f, 0.5f, 1f, 1f); // Light Blue
    [SerializeField] private Color activeColor = new Color(0f, 0f, 1f, 1f);      // Blue
    [SerializeField] private Color glowColor = new Color(0f, 1f, 1f, 1f);        // Cyan glow

    [Header("Animation Settings")]
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float glowPulseSpeed = 2f;
    [SerializeField] private float glowIntensity = 0.3f;

    [Header("References")]
    private CircleCollider2D triggerCollider;
    private Transform player;
    private Rigidbody2D playerRb;

    private bool isWarningPhase = true;
    private bool isActive = false;
    private Vector2 prisonCenter;
    private bool hasInitialized = false;

    void Awake()
    {
        // Initialize immediately when instantiated
        Initialize();
    }

    public void Initialize()
    {
        if (hasInitialized) return; // Prevent double initialization
        hasInitialized = true;

        Debug.Log("[PrisonPentagram] Initializing...");

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
            playerRb = playerObj.GetComponent<Rigidbody2D>();
            Debug.Log("[PrisonPentagram] Player found");
        }
        else
        {
            Debug.LogError("[PrisonPentagram] Player not found!");
        }

        prisonCenter = transform.position;

        // Start with transparent sprite
        if (pentagramRenderer != null)
        {
            Color startColor = warningColor;
            startColor.a = 0f;
            pentagramRenderer.color = startColor;
            Debug.Log("[PrisonPentagram] Sprite renderer found and set to transparent");
        }
        else
        {
            Debug.LogWarning("[PrisonPentagram] Pentagram Renderer not assigned!");
        }

        // Start sequence
        StartCoroutine(PrisonPentagramSequence());
    }

    private IEnumerator PrisonPentagramSequence()
    {
        Debug.Log("[PrisonPentagram] Starting sequence...");

        // Warning phase with fade in
        isWarningPhase = true;
        Debug.Log("[PrisonPentagram] Warning phase started");

        // Fade in during warning duration
        yield return StartCoroutine(FadeInPentagram(warningColor, warningDuration));

        Debug.Log("[PrisonPentagram] Warning phase complete, checking if player inside...");

        // Check if player escaped during warning
        if (!IsPlayerInside())
        {
            Debug.Log("[PrisonPentagram] Player escaped during warning! Destroying...");
            Destroy(gameObject);
            yield break;
        }

        // Activate prison
        isWarningPhase = false;
        isActive = true;
        Debug.Log("[PrisonPentagram] Activated! Trapping player...");

        // Start glow effect
        StartCoroutine(GlowPulse());

        // Trap duration
        float trapTimer = 0f;
        while (trapTimer < trapDuration)
        {
            if (player != null && playerRb != null)
            {
                // Keep player inside the pentagram
                KeepPlayerInside();
            }
            else
            {
                Debug.LogWarning("[PrisonPentagram] Player or PlayerRb is null during trap!");
                break;
            }

            trapTimer += Time.deltaTime;
            yield return null;
        }

        Debug.Log("[PrisonPentagram] Trap expired - destroying pentagram");
        Destroy(gameObject);
    }

    private IEnumerator FadeInPentagram(Color targetColor, float duration)
    {
        if (pentagramRenderer == null)
        {
            Debug.LogWarning("[PrisonPentagram] Cannot fade in - pentagramRenderer is null!");
            yield break;
        }

        float elapsed = 0f;
        Color startColor = targetColor;
        startColor.a = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Use animation curve for smooth fade
            float curveValue = fadeInCurve.Evaluate(t);
            
            Color newColor = targetColor;
            newColor.a = curveValue;
            pentagramRenderer.color = newColor;

            yield return null;
        }

        // Ensure we end at full alpha
        pentagramRenderer.color = targetColor;
        Debug.Log("[PrisonPentagram] Fade in complete");
    }

    private IEnumerator GlowPulse()
    {
        if (pentagramRenderer == null)
        {
            Debug.LogWarning("[PrisonPentagram] Cannot glow - pentagramRenderer is null!");
            yield break;
        }

        while (isActive && this != null && gameObject != null)
        {
            float pulse = Mathf.Sin(Time.time * glowPulseSpeed) * glowIntensity;
            pulse = (pulse + 1f) / 2f; // Normalize to 0-1 range

            // Blend between active color and glow color
            Color glowedColor = Color.Lerp(activeColor, glowColor, pulse);
            pentagramRenderer.color = glowedColor;

            yield return null;
        }
    }

    private void KeepPlayerInside()
    {
        if (player == null || playerRb == null) return;

        Vector2 playerPos = player.position;
        Vector2 directionFromCenter = playerPos - prisonCenter;
        float distanceFromCenter = directionFromCenter.magnitude;

        // If player is trying to leave, push them back
        if (distanceFromCenter > radius * 0.9f) // 90% of radius as buffer
        {
            Vector2 correctedPosition = prisonCenter + directionFromCenter.normalized * (radius * 0.85f);
            player.position = correctedPosition;
            
            // Stop their velocity trying to escape
            Vector2 velocityAwayFromCenter = Vector2.Dot(playerRb.linearVelocity, directionFromCenter.normalized) * directionFromCenter.normalized;
            if (Vector2.Dot(velocityAwayFromCenter, directionFromCenter) > 0) // Moving outward
            {
                playerRb.linearVelocity -= velocityAwayFromCenter;
            }
        }
    }

    private bool IsPlayerInside()
    {
        if (player == null)
        {
            Debug.LogWarning("[PrisonPentagram] Cannot check if player inside - player is null!");
            return false;
        }
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool inside = distanceToPlayer <= radius;
        Debug.Log($"[PrisonPentagram] Player distance: {distanceToPlayer}, radius: {radius}, inside: {inside}");
        return inside;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isWarningPhase ? Color.cyan : Color.blue;
        Vector3 center = Application.isPlaying ? prisonCenter : transform.position;
        Gizmos.DrawWireSphere(center, radius);
    }

    private void OnDestroy()
    {
        Debug.Log("[PrisonPentagram] Destroyed");
    }
}