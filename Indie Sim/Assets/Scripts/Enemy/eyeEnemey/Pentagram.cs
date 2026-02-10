using System.Collections;
using UnityEngine;

public enum PentagramType
{
    Escape,  // Player must escape or take damage
    Prison   // Player gets trapped inside
}

public class Pentagram : MonoBehaviour
{
    [Header("Pentagram Settings")]
    [SerializeField] private PentagramType pentagramType;
    [SerializeField] private float radius = 2f;

    [Header("Escape Pentagram Settings")]
    [SerializeField] private float escapeWarningDuration = 1.5f;
    [SerializeField] private int escapeDamage = 20;

    [Header("Prison Pentagram Settings")]
    [SerializeField] private float prisonWarningDuration = 0.8f; // Shorter warning
    [SerializeField] private float prisonTrapDuration = 3f;

    [Header("Escape Pentagram Visuals")]
    [SerializeField] private Sprite escapePentagramSprite;
    [SerializeField] private Color escapeWarningColor = new Color(1f, 1f, 0f, 1f); // Yellow
    [SerializeField] private Color escapeActiveColor = new Color(1f, 0f, 0f, 1f);   // Red
    [SerializeField] private Color escapeGlowColor = new Color(1f, 0.5f, 0f, 1f);   // Orange glow

    [Header("Prison Pentagram Visuals")]
    [SerializeField] private Sprite prisonPentagramSprite;
    [SerializeField] private Color prisonWarningColor = new Color(0.5f, 0.5f, 1f, 1f); // Light Blue
    [SerializeField] private Color prisonActiveColor = new Color(0f, 0f, 1f, 1f);      // Blue
    [SerializeField] private Color prisonGlowColor = new Color(0f, 1f, 1f, 1f);        // Cyan glow

    [Header("Animation Settings")]
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float glowPulseSpeed = 2f; // Speed of glow pulsing
    [SerializeField] private float glowIntensity = 0.3f; // How much the glow pulses (0-1)

    [Header("References")]
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D triggerCollider;
    private Transform player;
    private Rigidbody2D playerRb;

    private bool isWarningPhase = true;
    private bool isActive = false;
    private Vector2 prisonCenter; // Store center position for prison
    private Color currentBaseColor;
    private Color currentGlowColor;

    public void Initialize(bool isEscapeType)
    {
        pentagramType = isEscapeType ? PentagramType.Escape : PentagramType.Prison;
        
        // Setup visuals
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Set appropriate sprite
        if (pentagramType == PentagramType.Escape)
        {
            spriteRenderer.sprite = escapePentagramSprite;
            currentBaseColor = escapeWarningColor;
            currentGlowColor = escapeGlowColor;
        }
        else
        {
            spriteRenderer.sprite = prisonPentagramSprite;
            currentBaseColor = prisonWarningColor;
            currentGlowColor = prisonGlowColor;
        }

        // Start with transparent sprite
        Color startColor = currentBaseColor;
        startColor.a = 0f;
        spriteRenderer.color = startColor;

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
        }

        prisonCenter = transform.position;

        // Start appropriate coroutine
        if (pentagramType == PentagramType.Escape)
        {
            StartCoroutine(EscapePentagramSequence());
        }
        else
        {
            StartCoroutine(PrisonPentagramSequence());
        }
    }

    private IEnumerator EscapePentagramSequence()
    {
        // Warning phase with fade in
        isWarningPhase = true;
        Debug.Log("[Pentagram] ESCAPE - Warning phase started");

        // Fade in during warning duration
        yield return StartCoroutine(FadeInPentagram(escapeWarningColor, escapeWarningDuration));

        // Activate
        isWarningPhase = false;
        isActive = true;
        currentBaseColor = escapeActiveColor;
        currentGlowColor = escapeGlowColor;
        Debug.Log("[Pentagram] ESCAPE - Activated! Checking for player...");

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

    private IEnumerator PrisonPentagramSequence()
    {
        // Warning phase with fade in
        isWarningPhase = true;
        Debug.Log("[Pentagram] PRISON - Warning phase started");

        // Fade in during warning duration
        yield return StartCoroutine(FadeInPentagram(prisonWarningColor, prisonWarningDuration));

        // Check if player escaped during warning
        if (!IsPlayerInside())
        {
            Debug.Log("[Pentagram] PRISON - Player escaped during warning!");
            Destroy(gameObject);
            yield break;
        }

        // Activate prison
        isWarningPhase = false;
        isActive = true;
        currentBaseColor = prisonActiveColor;
        currentGlowColor = prisonGlowColor;
        Debug.Log("[Pentagram] PRISON - Activated! Trapping player...");

        // Start glow effect
        StartCoroutine(GlowPulse());

        // Trap duration
        float trapTimer = 0f;
        while (trapTimer < prisonTrapDuration)
        {
            if (player != null && playerRb != null)
            {
                // Keep player inside the pentagram
                KeepPlayerInside();
            }

            trapTimer += Time.deltaTime;
            yield return null;
        }

        Debug.Log("[Pentagram] PRISON - Trap expired");
        Destroy(gameObject);
    }

    private IEnumerator FadeInPentagram(Color targetColor, float duration)
    {
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
            spriteRenderer.color = newColor;

            yield return null;
        }

        // Ensure we end at full alpha
        spriteRenderer.color = targetColor;
    }

    private IEnumerator GlowPulse()
    {
        while (isActive)
        {
            float pulse = Mathf.Sin(Time.time * glowPulseSpeed) * glowIntensity;
            pulse = (pulse + 1f) / 2f; // Normalize to 0-1 range

            // Blend between base color and glow color
            Color glowedColor = Color.Lerp(currentBaseColor, currentGlowColor, pulse);
            spriteRenderer.color = glowedColor;

            yield return null;
        }
    }

    private void KeepPlayerInside()
    {
        Vector2 playerPos = player.position;
        Vector2 directionFromCenter = playerPos - prisonCenter;
        float distanceFromCenter = directionFromCenter.magnitude;

        // If player is trying to leave, push them back
        if (distanceFromCenter > radius * 0.9f) // 90% of radius as buffer
        {
            Vector2 correctedPosition = prisonCenter + directionFromCenter.normalized * (radius * 0.85f);
            player.position = correctedPosition;
            
            // Stop their velocity trying to escape
            if (playerRb != null)
            {
                Vector2 velocityAwayFromCenter = Vector2.Dot(playerRb.linearVelocity, directionFromCenter.normalized) * directionFromCenter.normalized;
                if (Vector2.Dot(velocityAwayFromCenter, directionFromCenter) > 0) // Moving outward
                {
                    playerRb.linearVelocity -= velocityAwayFromCenter;
                }
            }
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
        if (player == null) return;

        IDamageable playerDamageable = player.GetComponent<IDamageable>();
        if (playerDamageable != null)
        {
            playerDamageable.TakeDamage(escapeDamage);
            Debug.Log($"[Pentagram] ESCAPE - Damaged player for {escapeDamage}");
        }
    }

    private void OnDrawGizmos()
    {
        // Draw pentagram radius
        if (isWarningPhase)
        {
            Gizmos.color = Color.yellow;
        }
        else if (pentagramType == PentagramType.Escape)
        {
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = Color.blue;
        }

        Gizmos.DrawWireSphere(transform.position, radius);
    }
}