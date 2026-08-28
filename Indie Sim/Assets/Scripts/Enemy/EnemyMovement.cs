using UnityEngine;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2.5f;
    public float rotationSpeed = 5f;

    [Header("Spring Attack Settings")]
    [SerializeField] private float attackTriggerDistance = 3f;
    [SerializeField] private float windupDistance = 0.5f;
    [SerializeField] private float lungeForce = 15f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float lungeDrag = 5f;

    [Header("Juice Settings")]
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private float squashAmount = 0.7f;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 1.2f;   // How close before pushing away
    [SerializeField] private float separationForce = 3f;      // How hard they push each other

    private Rigidbody2D rb;
    private Transform player;
    private bool isActivated = false;
    private bool isAttacking = false;
    private float nextAttackTime = 0f;
    private Vector3 originalScale;

    // Set explicitly by PlayerController each frame it bulldozes this enemy.
    // Using an explicit flag instead of a velocity heuristic avoids script
    // execution order races where FixedUpdate could stomp the pushed velocity.
    private bool isExternallyPushed = false;
    private int externalPushFramesRemaining = 0;
    private const int PUSH_LINGER_FRAMES = 3;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();

        if (spriteTransform == null) spriteTransform = transform;
        originalScale = spriteTransform.localScale;
    }

    /// <summary>
    /// Called by PlayerController every FixedUpdate frame it is bulldozing this enemy.
    /// </summary>
    public void NotifyBulldozed()
    {
        isExternallyPushed = true;
        externalPushFramesRemaining = PUSH_LINGER_FRAMES;
    }

    void FixedUpdate()
    {
        if (ActivateEnemies.Instance != null)
            isActivated = ActivateEnemies.Instance.IsEnemyActivated(gameObject);

        if (!isActivated || player == null) return;

        // Count down linger frames so the push has time to actually move the enemy
        // before we hand control back to chase AI.
        if (externalPushFramesRemaining > 0)
        {
            externalPushFramesRemaining--;
            isExternallyPushed = true;
        }
        else
        {
            isExternallyPushed = false;
        }

        if (isAttacking || isExternallyPushed) return;

        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackTriggerDistance && Time.time >= nextAttackTime)
        {
            StartCoroutine(SpringAttack(direction));
            return;
        }

        ApplySeparation();
        rb.linearVelocity = direction * speed;
        RotateTowards(direction);
    }

    private IEnumerator SpringAttack(Vector2 dirToPlayer)
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        // --- 1. Wind-up with Squash Juice ---
        Vector2 startPos = transform.position;
        Vector2 windupPos = (Vector2)transform.position - (dirToPlayer * windupDistance);
        float elapsed = 0f;
        float windupDuration = 0.4f;

        while (elapsed < windupDuration)
        {
            // Bulldozed mid-windup — abort cleanly
            if (isExternallyPushed)
            {
                if (spriteTransform != null) spriteTransform.localScale = originalScale;
                isAttacking = false;
                nextAttackTime = Time.time + attackCooldown * 0.5f;
                yield break;
            }

            float t = elapsed / windupDuration;

            rb.MovePosition(Vector2.Lerp(startPos, windupPos, t));

            if (spriteTransform != null)
            {
                spriteTransform.localScale = new Vector3(
                    originalScale.x * (1 + (1 - squashAmount) * t),
                    originalScale.y * Mathf.Lerp(1f, squashAmount, t),
                    originalScale.z
                );
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // --- 2. Lunge ---
        spriteTransform.localScale = originalScale;

        Vector2 lungeDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.AddForce(lungeDir * lungeForce, ForceMode2D.Impulse);

        // --- 3. Deceleration ---
        float lungeTimer = 0f;
        float maxLungeDuration = 0.6f;

        while (lungeTimer < maxLungeDuration)
        {
            // Bulldozed mid-lunge — yield and let the push take over
            if (isExternallyPushed)
            {
                isAttacking = false;
                nextAttackTime = Time.time + attackCooldown;
                yield break;
            }

            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * lungeDrag);

            lungeTimer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    private void ApplySeparation()
    {
        // Push away from any other enemy that is too close
        Collider2D[] neighbours = Physics2D.OverlapCircleAll(transform.position, separationRadius);
        foreach (Collider2D col in neighbours)
        {
            if (col.gameObject == gameObject) continue;
            if (col.GetComponent<EnemyMovement>() == null) continue;

            Vector2 away = (Vector2)(transform.position - col.transform.position);
            float dist = away.magnitude;
            if (dist < 0.01f) away = Random.insideUnitCircle.normalized; // exact overlap fallback
            else away /= dist; // normalize

            // Stronger push the closer they are
            float strength = Mathf.InverseLerp(separationRadius, 0f, dist);
            rb.AddForce(away * separationForce * strength, ForceMode2D.Force);
        }
    }

    private void RotateTowards(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.1f) return;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, angle), rotationSpeed * Time.deltaTime);
    }

    public void ApplyKnockback(Vector2 force)
    {
        StopAllCoroutines();
        isAttacking = false;
        if (spriteTransform != null) spriteTransform.localScale = originalScale;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);
    }
}