using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour, IDamageable
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab; // Enemy prefab to spawn
    public float spawnRadius = 5f; // Radius within which enemies will spawn
    public float spawnInterval = 3f; // Time between spawns
    public int maxEnemies = 10; // Max number of enemies at a time

    [Header("Respawn Reference")]
    public DungeonMapGenerator mapGenerator;

    [Header("Health System")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Damage Flash Effect")]
    [SerializeField] private float flashDuration = 0.15f; // How long the flash lasts
    [SerializeField] private Color flashColor = Color.white; // Flash color

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip deathSound;

    // Components
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isFlashing = false;
    private bool isDead = false;

    // Spawning tracking
    private int currentEnemyCount = 0;

    // Events
    public System.Action<int, int> OnHealthChanged; // current, max
    public System.Action OnDeath;

    void Start()
    {
        InitializeSpawner();
        InvokeRepeating(nameof(SpawnEnemy), spawnInterval, spawnInterval);
    }

    private void InitializeSpawner()
    {
        // Initialize health
        currentHealth = maxHealth;

        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        // Store original color for flash effect
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            // If no SpriteRenderer, add one for visual feedback
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = Color.blue; // Default spawner color
            originalColor = spriteRenderer.color;
        }

        // Add trigger collider if not present (for cone damage detection)
        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = false; // Set to false for cone detection to work
            collider.radius = 1f; // Adjust as needed
        }

        Debug.Log($"EnemySpawner initialized with {maxHealth} health");
    }

    void SpawnEnemy()
    {
        // Don't spawn if dead
        if (isDead) return;

        if (currentEnemyCount >= maxEnemies) return;

        // Get a random position within the circular area
        Vector2 randomPosition = GetRandomPosition();

        // Instantiate enemy
        GameObject enemy = Instantiate(enemyPrefab, randomPosition, Quaternion.identity);
        currentEnemyCount++;

        // Subscribe to enemy death event
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.OnDeath += EnemyDied;
        }

        Debug.Log($"Enemy spawned at {randomPosition}. Total enemies: {currentEnemyCount}");
    }

    Vector2 GetRandomPosition()
    {
        // Get a random angle
        float angle = Random.Range(0f, 2f * Mathf.PI);
        // Get a random distance within the spawn radius
        float distance = Random.Range(0f, spawnRadius);
        // Convert polar coordinates to Cartesian coordinates
        float x = transform.position.x + Mathf.Cos(angle) * distance;
        float y = transform.position.y + Mathf.Sin(angle) * distance;
        return new Vector2(x, y);
    }

    void EnemyDied()
    {
        currentEnemyCount--;
        Debug.Log($"Enemy died. Remaining enemies: {currentEnemyCount}");
    }

    #region IDamageable Interface Implementation

    // INTERFACE METHOD: Must match exactly - public void TakeDamage(int damage)
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        // Reduce health
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Trigger flash effect
        if (!isFlashing)
        {
            StartCoroutine(FlashWhite());
        }

        // Play damage sound
        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        // Notify health change
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"EnemySpawner took {damage} damage. Health: {currentHealth}/{maxHealth}");

        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // INTERFACE METHOD: Must match exactly - public bool IsDead()
    public bool IsDead()
    {
        return isDead;
    }

    // INTERFACE METHOD: Must match exactly - public GameObject GetGameObject()
    public GameObject GetGameObject()
    {
        return gameObject;
    }

    #endregion

    #region Health System

    public void Heal(int healAmount)
    {
        if (isDead) return;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"EnemySpawner healed {healAmount}. Health: {currentHealth}/{maxHealth}");
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        // Stop spawning enemies
        CancelInvoke(nameof(SpawnEnemy));

        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Notify death
        OnDeath?.Invoke();

        Debug.Log("EnemySpawner destroyed!");

                // Notify map generator for respawn
        if (mapGenerator != null)
        {
            mapGenerator.OnSpawnerDestroyed(transform.position);
        }
        // Start destruction sequence
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Flash effect on death
        if (!isFlashing)
        {
            StartCoroutine(FlashWhite());
        }

        // Wait a moment before destroying
        yield return new WaitForSeconds(1f);

        // Destroy the spawner
        Destroy(gameObject);
    }

    #endregion

    #region Visual Effects

    private IEnumerator FlashWhite()
    {
        if (spriteRenderer == null || isFlashing) yield break;

        isFlashing = true;

        // Flash to white/flash color
        spriteRenderer.color = flashColor;

        // Wait for flash duration
        yield return new WaitForSeconds(flashDuration);

        // Return to original color
        spriteRenderer.color = originalColor;

        isFlashing = false;
    }

    #endregion

    #region Public API

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
    public bool IsAtFullHealth() => currentHealth >= maxHealth;
    public int GetCurrentEnemyCount() => currentEnemyCount;

    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetSpawnRate(float newSpawnInterval)
    {
        spawnInterval = Mathf.Max(0.1f, newSpawnInterval);

        // Restart spawning with new interval
        CancelInvoke(nameof(SpawnEnemy));
        if (!isDead)
        {
            InvokeRepeating(nameof(SpawnEnemy), spawnInterval, spawnInterval);
        }
    }

    public void SetMaxEnemies(int newMaxEnemies)
    {
        maxEnemies = Mathf.Max(1, newMaxEnemies);
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        // Draw spawn radius
        Gizmos.color = isDead ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        // Draw health bar above spawner (only in play mode)
        if (Application.isPlaying && maxHealth > 0)
        {
            Vector3 healthBarPos = transform.position + Vector3.up * 2f;
            float healthPercentage = GetHealthPercentage();

            // Background (red)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(healthBarPos - Vector3.right * 1f, healthBarPos + Vector3.right * 1f);

            // Health (green)
            Gizmos.color = Color.green;
            Vector3 healthEnd = healthBarPos + Vector3.right * (healthPercentage * 2f - 1f);
            Gizmos.DrawLine(healthBarPos - Vector3.right * 1f, healthEnd);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Highlight when selected
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        // Show max spawn area
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius * 1.5f);
    }

    #endregion
}
