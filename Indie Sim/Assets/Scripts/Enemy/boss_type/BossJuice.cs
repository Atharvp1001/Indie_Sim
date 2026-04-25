using System.Collections;
using UnityEngine;

public class BossJuice : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spriteTransform; 
    private Rigidbody2D rb;

    [Header("Squash & Stretch Settings")]
    [SerializeField] private float squashAmount = 0.3f;  // Flattening factor
    [SerializeField] private float stretchAmount = 0.2f; // Bulge factor
    [SerializeField] private float squashDuration = 0.08f;
    [SerializeField] private float recoverDuration = 0.15f;
    [SerializeField] private AnimationCurve recoveryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 originalScale;
    private Coroutine squashCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Failsafe to find sprite child if not assigned
        if (spriteTransform == null)
            spriteTransform = GetComponentInChildren<SpriteRenderer>().transform;
        
        originalScale = spriteTransform.localScale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check 1: Ignore if boss is dead or already kinematic (stuck to wall)
        if (rb == null || rb.bodyType == RigidbodyType2D.Kinematic) return;

        // Check 2: Ensure we hit a wall/solid object
        if (collision.contacts.Length > 0)
        {
            Vector2 impactNormal = collision.contacts[0].normal;
            
            if (squashCoroutine != null) StopCoroutine(squashCoroutine);
            squashCoroutine = StartCoroutine(SquashRoutine(impactNormal));
        }
    }

    private IEnumerator SquashRoutine(Vector2 normal)
    {
        // Check 3: Failsafe for sprite reference
        if (spriteTransform == null) yield break;

        // Calculate rotation to align the squash with the wall impact
        // We temporarily rotate the sprite local axis so Y faces the wall
        float impactAngle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;
        Quaternion impactRotation = Quaternion.Euler(0, 0, impactAngle);
        
        // To prevent snapping, we only rotate the 'squash axis', not the whole boss
        spriteTransform.rotation = impactRotation;

        float elapsed = 0f;

        // --- SQUASH PHASE ---
        while (elapsed < squashDuration)
        {
            // If the boss becomes kinematic mid-squash (sticks to wall), abort
            if (rb.bodyType == RigidbodyType2D.Kinematic) break;

            elapsed += Time.deltaTime;
            float t = elapsed / squashDuration;
            
            float currentSquash = Mathf.Lerp(1f, 1f - squashAmount, t);
            float currentStretch = Mathf.Lerp(1f, 1f + stretchAmount, t);
            
            spriteTransform.localScale = new Vector3(
                originalScale.x * currentStretch, 
                originalScale.y * currentSquash, 
                originalScale.z
            );
            yield return null;
        }

        // --- RECOVERY PHASE ---
        elapsed = 0f;
        while (elapsed < recoverDuration)
        {
            // If the boss becomes kinematic mid-recovery, reset immediately and exit
            if (rb.bodyType == RigidbodyType2D.Kinematic) break;

            elapsed += Time.deltaTime;
            float t = recoveryCurve.Evaluate(elapsed / recoverDuration);
            
            float currentSquash = Mathf.Lerp(1f - squashAmount, 1f, t);
            float currentStretch = Mathf.Lerp(1f + stretchAmount, 1f, t);
            
            spriteTransform.localScale = new Vector3(
                originalScale.x * currentStretch, 
                originalScale.y * currentSquash, 
                originalScale.z
            );
            yield return null;
        }

        // Final Reset
        spriteTransform.localScale = originalScale;
        
        // Reset rotation only if we aren't stuck (BossAnimator handles rotation when stuck)
        if (rb.bodyType != RigidbodyType2D.Kinematic)
        {
            spriteTransform.localRotation = Quaternion.identity;
        }
    }
}