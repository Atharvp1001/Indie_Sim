using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float speed = 2f; // Movement speed
    private Transform player;
    private Rigidbody2D rb;
    private Vector2 movement;
    private bool isKnockedBack = false; // Prevents movement when knocked back
    private float knockbackRecoveryTime = 0.5f; // Time before enemy resumes movement
    private float knockbackTimer = 0f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform; // Finds player by tag
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (player == null) return; // Prevent errors if player is missing

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
    }

    void FixedUpdate()
    {
        if (!isKnockedBack)
        {
            rb.linearVelocity = movement; // Move towards player
        }
    }

    public void ApplyKnockback(Vector2 force)
    {
        rb.linearVelocity = Vector2.zero; // Reset velocity
        rb.AddForce(force, ForceMode2D.Impulse); // Apply knockback
        isKnockedBack = true;
        knockbackTimer = knockbackRecoveryTime; // Set timer to resume movement
    }
}
