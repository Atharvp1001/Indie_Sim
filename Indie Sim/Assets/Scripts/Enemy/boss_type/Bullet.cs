using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("Collision Layers")]
    [SerializeField] private LayerMask damageableLayers; // Player layer
    [SerializeField] private LayerMask destructionLayers; // Walls layer

    private int damage;
    private float lifetime;
    private float spawnTime;
    private bool hasHit = false;
    private BulletPool pool;
    private Vector2 velocity;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(int bulletDamage, float bulletLifetime, BulletPool bulletPool, Vector2 bulletVelocity)
    {
        damage = bulletDamage;
        lifetime = bulletLifetime;
        spawnTime = Time.time;
        hasHit = false;
        pool = bulletPool;
        velocity = bulletVelocity;

        transform.rotation = Quaternion.identity;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            Vector2 newPosition = rb.position + velocity * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }

        if (Time.time >= spawnTime + lifetime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        // Ignore collisions for first 0.15 seconds
        if (Time.time < spawnTime + 0.15f)
        {
            return;
        }

        int collisionLayer = collision.gameObject.layer;

        // Check if hit damageable (player)
        if (IsInLayerMask(collisionLayer, damageableLayers))
        {
            IDamageable damageable = collision.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead())
            {
                damageable.TakeDamage(damage);
                hasHit = true;
                ReturnToPool();
                return;
            }

            // Fallback to PlayerHealth
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, transform.position);
                hasHit = true;
                ReturnToPool();
                return;
            }
        }

        // Check if hit wall/obstacle
        if (IsInLayerMask(collisionLayer, destructionLayers))
        {
            hasHit = true;
            ReturnToPool();
            return;
        }
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return layerMask == (layerMask | (1 << layer));
    }

    private void ReturnToPool()
    {
        if (pool != null)
        {
            pool.ReturnBullet(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}