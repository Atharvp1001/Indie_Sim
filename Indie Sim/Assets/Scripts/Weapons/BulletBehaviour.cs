using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    [SerializeField] private float speed = 10f; // Bullet speed
    [SerializeField] private int damage = 20; // Bullet damage
    [SerializeField] private float lifetime = 2f; // Destroy after time
    [SerializeField] private float knockbackForce = 5f; // Knockback force applied to enemies
    [SerializeField] private LayerMask enemyLayer; // Define enemy layer to detect collisions

    private Vector2 direction;

    void Start()
    {
        Destroy(gameObject, lifetime); // Auto-destroy bullet after some time
    }

    public void SetDirection(Vector2 targetDirection)
    {
        direction = targetDirection.normalized; // Normalize direction
        GetComponent<Rigidbody2D>().linearVelocity = direction * speed; // Apply velocity
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null)
        {
            // Knockback direction (away from bullet)
            Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;

            // Apply damage and knockback
            enemy.TakeDamage(damage, knockbackDirection * knockbackForce);

            Destroy(gameObject); // Destroy bullet on impact
        }
    }
}
