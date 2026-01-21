using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float speed = 2f; // Movement speed
    private Transform player;
    private Rigidbody2D rb;
    private Vector2 movement;
    private bool isKnockedBack = false; // Prevents movement when knocked back
    private float knockbackRecoveryTime = 0.2f; // Time before enemy resumes movement
    private float knockbackTimer = 0f;
    
    [Header("Knockback Settings")]
    [SerializeField] private float normalDrag = 0f; // Normal movement drag
    [SerializeField] private float knockbackDrag = 15f; // High drag during knockback for quick stop

    // NEW: Activation check
    private bool isActivated = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform; // Finds player by tag
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (player == null) return; // Prevent errors if player is missing

        // NEW: Check if this enemy should be activated
        if (!isActivated && ActivateEnemies.Instance != null)
        {
            isActivated = ActivateEnemies.Instance.IsEnemyActivated(gameObject);
            if (!isActivated) return; // Don't move if not activated yet
        }

        if (!isKnockedBack) // Only move if not knocked back
        {
            Vector2 direction = (player.position - transform.position).normalized;
            movement = direction * speed;
        }

        // Countdown to recover from knockback
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
            }
        }
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;

            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
                rb.linearVelocity = Vector2.zero; // Snap to stop
                rb.linearDamping = normalDrag; // Reset to normal drag
            }
        }
    }

    void FixedUpdate()
    {
        // Only move if activated and not in hit-stun
        if (!isActivated || isKnockedBack) return;

        // MARBLE FIX: Gradually move towards player instead of snapping velocity
        // This allows external forces (like the player pushing) to work
        Vector2 targetVelocity = movement; 
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 5f);

        // CROWD FIX: Push away from other enemies so they don't clump
        HandleCrowdSeparation();
    }
    private static Collider2D[] neighborResults = new Collider2D[3]; // Small array for performance

    void HandleCrowdSeparation()
    {
        // Optimization: Only check for neighbors every 2nd frame
        if (Time.frameCount % 2 != 0) return;

        // Use a small radius (0.5 - 0.7) to detect nearby "friend" enemies
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, 0.6f, neighborResults);
        for (int i = 0; i < count; i++)
        {
            if (neighborResults[i].gameObject == gameObject) continue;

            // Apply a small shove away from other enemies
            Vector2 shoveDir = (transform.position - neighborResults[i].transform.position).normalized;
            rb.AddForce(shoveDir * 5f); // 5f is a good starting strength
        }
    }
    public void ApplyKnockback(Vector2 force)
    {
        rb.linearVelocity = Vector2.zero; // Reset velocity

        // Temporarily increase drag for punchy, short knockback
        rb.linearDamping = knockbackDrag; // High drag = quick stop

        rb.AddForce(force, ForceMode2D.Impulse); // Apply knockback
        isKnockedBack = true;
        knockbackTimer = knockbackRecoveryTime; // Set timer to resume movement
    }

}
