using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossEnemy : MonoBehaviour, IDamageable
{
    [Header("Boss Spawning Requirements")]
    [SerializeField] private int minTeleporterUses = 2;
    [SerializeField] private int maxTeleporterUses = 5;
    [SerializeField] private float maxSpawnTimeSeconds = 180f; // 3 minutes default

    [Header("Boss Stats")]
    [SerializeField] private int maxHealth = 500;
    [SerializeField] private int attackDamage = 10; // Same as normal enemy
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Pinball Movement")]
    [SerializeField] private float moveSpeed = 4f; // Slower than normal enemy
    [SerializeField] private float wallBounceMultiplier = 1.2f;
    [SerializeField] private float directionChangeInterval = 2f; // Recalculate path to player

    [Header("Layer Settings")]
    [SerializeField] private LayerMask wallsLayer; // Assign your Walls layer in inspector

    [Header("Visual Feedback")]
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color damageColor = Color.red;

    [Header("Loot Drop")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int minCoins = 10;
    [SerializeField] private int maxCoins = 20;
    [SerializeField] private float coinDropForce = 5f;
    [SerializeField] private float coinSpreadRadius = 2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip bounceSound;

    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;
    
    // State
    private int currentHealth;
    private bool isDead = false;
    private float lastAttackTime = 0f;
    private Color originalColor;
    private Coroutine flashCoroutine;
    private Vector2 moveDirection;
    private float nextDirectionChangeTime;

    // Events
    public System.Action OnDeath;
    public System.Action<int, int> OnHealthChanged;

    void Start()
    {
        InitializeBoss();
    }

    private void InitializeBoss()
    {
        currentHealth = maxHealth;
        
        // Get components
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        // Configure rigidbody for pinball movement
        rb.linearDamping = 0.5f; // Some drag so it doesn't go crazy
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            UpdateMoveDirection();
        }

        // Store original color
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Set first direction change
        nextDirectionChangeTime = Time.time + directionChangeInterval;

        Debug.Log("Boss Enemy spawned!");
    }

    void Update()
    {
        if (isDead || playerTransform == null) return;

        // Update direction towards player periodically
        if (Time.time >= nextDirectionChangeTime)
        {
            UpdateMoveDirection();
            nextDirectionChangeTime = Time.time + directionChangeInterval;
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // Move in current direction
        rb.linearVelocity = moveDirection * moveSpeed;
    }

    private void UpdateMoveDirection()
    {
        if (playerTransform != null)
        {
            moveDirection = (playerTransform.position - transform.position).normalized;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Bounce off walls using layer check
        if (IsOnLayer(collision.gameObject, wallsLayer))
        {
            BounceOffWall(collision);
        }

        // Attack player
        if (collision.gameObject.CompareTag("Player"))
        {
            AttemptAttackPlayer(collision.gameObject);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Keep attacking player if touching
        if (collision.gameObject.CompareTag("Player"))
        {
            AttemptAttackPlayer(collision.gameObject);
        }
    }

    /// <summary>
    /// Checks if a GameObject is on any of the layers in the LayerMask
    /// </summary>
    private bool IsOnLayer(GameObject obj, LayerMask layerMask)
    {
        return ((1 << obj.layer) & layerMask) != 0;
    }

    private void BounceOffWall(Collision2D collision)
    {
        // Get collision normal
        Vector2 normal = collision.contacts[0].normal;
        
        // Reflect direction
        moveDirection = Vector2.Reflect(moveDirection, normal).normalized;
        
        // Apply bounce velocity
        rb.linearVelocity = moveDirection * moveSpeed * wallBounceMultiplier;

        // Play bounce sound
        if (audioSource != null && bounceSound != null)
        {
            audioSource.PlayOneShot(bounceSound, 0.3f);
        }

        Debug.Log("Boss bounced off wall!");
    }

    private void AttemptAttackPlayer(GameObject player)
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage, transform.position);
                lastAttackTime = Time.time;
                Debug.Log($"Boss attacked player for {attackDamage} damage");
            }
        }
    }

    #region IDamageable Implementation

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Flash effect
        if (spriteRenderer != null)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashDamage());
        }

        // Play damage sound
        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"Boss took {damage} damage. Health: {currentHealth}/{maxHealth}");

        // NO KNOCKBACK - Boss is too powerful!

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public bool IsDead() => isDead;
    public GameObject GetGameObject() => gameObject;

    #endregion

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Stop movement
        rb.linearVelocity = Vector2.zero;

        // Drop coins
        DropCoins();

        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        OnDeath?.Invoke();

        Debug.Log("Boss defeated!");

        // Destroy after delay
        Destroy(gameObject, 2f);
    }

    private void DropCoins()
    {
        if (coinPrefab == null) return;

        int coinCount = Random.Range(minCoins, maxCoins + 1);

        for (int i = 0; i < coinCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * coinSpreadRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            GameObject coin = Instantiate(coinPrefab, spawnPosition, Quaternion.identity);

            Rigidbody2D coinRb = coin.GetComponent<Rigidbody2D>();
            if (coinRb != null)
            {
                Vector2 randomForce = new Vector2(
                    Random.Range(-coinDropForce, coinDropForce),
                    Random.Range(coinDropForce * 0.5f, coinDropForce)
                );
                coinRb.AddForce(randomForce, ForceMode2D.Impulse);
                coinRb.AddTorque(Random.Range(-5f, 5f), ForceMode2D.Impulse);
            }
        }
    }

    private IEnumerator FlashDamage()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(flashDuration);
            if (!isDead) spriteRenderer.color = originalColor;
        }
        flashCoroutine = null;
    }

    // Public getters
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
    public int GetMinTeleporterUses() => minTeleporterUses;
    public int GetMaxTeleporterUses() => maxTeleporterUses;
    public float GetMaxSpawnTime() => maxSpawnTimeSeconds;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw health bar
        Vector3 healthBarPos = transform.position + Vector3.up * 2.5f;
        float healthPercentage = GetHealthPercentage();

        Gizmos.color = Color.red;
        Gizmos.DrawLine(healthBarPos - Vector3.right * 1f, healthBarPos + Vector3.right * 1f);

        Gizmos.color = Color.yellow;
        Vector3 healthEnd = healthBarPos + Vector3.right * (healthPercentage * 2f - 1f);
        Gizmos.DrawLine(healthBarPos - Vector3.right * 1f, healthEnd);

        // Draw boss indicator
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}