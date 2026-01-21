using UnityEngine;

public class BulletProjectile : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float bulletSpeed = 8f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifetime = 5f; // Auto-destroy after 5 seconds if no collision

    [Header("Visual (Optional)")]
    [SerializeField] private GameObject hitEffect; // Particle effect on impact

    private Vector2 direction;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Auto-destroy after lifetime to prevent infinite bullets
        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// Call this when spawning the bullet to set its direction
    /// </summary>
    public void Initialize(Vector2 shootDirection)
    {
        direction = shootDirection.normalized;
        
        // Set velocity
        if (rb != null)
        {
            rb.linearVelocity = direction * bulletSpeed;
        }

        // Rotate bullet sprite to face direction (optional, looks nice)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Hit the player - deal damage
        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, transform.position);
                Debug.Log($"Bullet hit player for {damage} damage");
            }

            DestroyBullet();
            return;
        }

        // Hit a wall - just destroy
        if (collision.CompareTag("Wall") || collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            Debug.Log("Bullet hit wall");
            DestroyBullet();
            return;
        }

        // Hit another enemy - destroy but DON'T damage
        if (collision.CompareTag("Enemy"))
        {
            Debug.Log("Bullet hit another enemy (no damage)");
            DestroyBullet();
            return;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Backup collision detection in case trigger doesn't work
        // Same logic as OnTriggerEnter2D
        
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, transform.position);
            }
            DestroyBullet();
            return;
        }

        // Destroy on any other collision (walls, enemies, etc)
        DestroyBullet();
    }

    void DestroyBullet()
    {
        // Spawn hit effect if assigned
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)direction * 0.5f);
    }
}