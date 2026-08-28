using System.Collections;
using UnityEngine;

public class TriangleEnemyJuice : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spriteRoot;
    private Rigidbody2D rb;
    private Transform player;

    [Header("Squeeze Settings")]
    [SerializeField] private float squeezeDuration = 0.2f;
    [SerializeField] private float squashAmount = 0.7f; // How much it flattens (length)
    [SerializeField] private float stretchAmount = 1.2f; // How much it widens (width)

    [Header("Recoil Settings")]
    [SerializeField] private float recoilForce = 5f;
    [SerializeField] private float recoilDrag = 10f; // Snaps the recoil to a stop

    private Vector3 originalScale;
    private Coroutine juiceRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (spriteRoot != null)
            originalScale = spriteRoot.localScale;
    }

    public void OnFired()
    {
        if (juiceRoutine != null) StopCoroutine(juiceRoutine);
        juiceRoutine = StartCoroutine(HandleJuice());
    }

    private IEnumerator HandleJuice()
    {
        if (spriteRoot == null || rb == null || player == null) yield break;

        // 1. Calculate Recoil Direction (Opposite of Player)
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        Vector2 recoilDir = -dirToPlayer;

        // 2. Apply Snappy Recoil Impulse
        rb.linearVelocity = Vector2.zero; // Clear movement velocity for the "kick"
        rb.AddForce(recoilDir * recoilForce, ForceMode2D.Impulse);

        // 3. Squeeze and Recover
        float elapsed = 0f;
        
        // Note: We use the same squash logic as your other enemy
        // This compresses the Y (forward axis) and stretches X (sides)
        while (elapsed < squeezeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / squeezeDuration;
            
            // Curve the squeeze: rapid squash, smooth recovery
            float curve = Mathf.Sin(t * Mathf.PI); 

            float currentSquash = Mathf.Lerp(1f, squashAmount, curve);
            float currentStretch = Mathf.Lerp(1f, stretchAmount, curve);

            spriteRoot.localScale = new Vector3(
                originalScale.x * currentStretch, 
                originalScale.y * currentSquash, 
                originalScale.z
            );

            // Apply drag to the recoil movement so it doesn't drift forever
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * recoilDrag);

            yield return null;
        }

        // Reset
        spriteRoot.localScale = originalScale;
        rb.linearVelocity = Vector2.zero;
        juiceRoutine = null;
    }
}