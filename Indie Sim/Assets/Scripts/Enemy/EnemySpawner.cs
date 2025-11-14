using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour, IDamageable
{
    [Header("Enemy Prefabs - Different Difficulty Levels")]
    [Tooltip("Level 1 Enemy (easiest)")]
    public GameObject enemyLevel1Prefab;

    [Tooltip("Level 2 Enemy (medium)")]
    public GameObject enemyLevel2Prefab;

    [Tooltip("Level 3 Enemy (hardest)")]
    public GameObject enemyLevel3Prefab;

    [Header("Spawner Settings")]
    public float spawnRadius = 5f; // Radius within which enemies will spawn
    public float spawnInterval = 3f; // Time between spawns
    public int maxEnemies = 5; // Max number of enemies at a time

    [Header("Difficulty Scaling")]
    [Tooltip("Current dungeon number (set by RoguelikeManager)")]
    [SerializeField] private int currentDungeonLevel = 1;

    [Header("Health System")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Damage Flash Effect")]
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private Color flashColor = Color.white;

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

    // Spawn rate cache (calculated once per dungeon)
    private float spawnRate_Level1 = 80f;
    private float spawnRate_Level2 = 15f;
    private float spawnRate_Level3 = 5f;

    // Events
    public System.Action<int, int> OnHealthChanged;
    public System.Action OnDeath;

    void Start()
    {
        InitializeSpawner();

        // Calculate spawn rates for current dungeon
        CalculateSpawnRates(currentDungeonLevel);

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
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = Color.blue;
            originalColor = spriteRenderer.color;
        }

        // Add trigger collider if not present
        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = false;
            collider.radius = 1f;
        }
    }

    /// <summary>
    /// Calculates spawn rates for each enemy level based on dungeon number
    /// S1 = 80 - 10(D-1)
    /// S2 = 15 + 5(D-1)
    /// S3 = 5 + 5(D-1)
    /// </summary>
    private void CalculateSpawnRates(int dungeonNumber)
    {
        // Clamp dungeon number to valid range (1-9)
        // After dungeon 9, rates would be: 0% - 55% - 45%
        dungeonNumber = Mathf.Clamp(dungeonNumber, 1, 9);

        // Apply formulas
        spawnRate_Level1 = 80f - 10f * (dungeonNumber - 1);
        spawnRate_Level2 = 15f + 5f * (dungeonNumber - 1);
        spawnRate_Level3 = 5f + 5f * (dungeonNumber - 1);

        // Clamp rates to valid ranges
        spawnRate_Level1 = Mathf.Max(spawnRate_Level1, 0f);
        spawnRate_Level2 = Mathf.Clamp(spawnRate_Level2, 0f, 100f);
        spawnRate_Level3 = Mathf.Clamp(spawnRate_Level3, 0f, 100f);

        Debug.Log($"[EnemySpawner] Spawn rates for Dungeon {dungeonNumber}:");
        Debug.Log($"  Level 1: {spawnRate_Level1}%");
        Debug.Log($"  Level 2: {spawnRate_Level2}%");
        Debug.Log($"  Level 3: {spawnRate_Level3}%");
    }

    /// <summary>
    /// Spawns an enemy based on weighted probability
    /// Uses cumulative distribution for selection
    /// </summary>
    void SpawnEnemy()
    {
        if (isDead) return;
        if (currentEnemyCount >= maxEnemies) return;

        // Select which enemy to spawn based on spawn rates
        GameObject selectedEnemyPrefab = SelectEnemyByWeight();

        if (selectedEnemyPrefab == null)
        {
            Debug.LogWarning("[EnemySpawner] No enemy prefab selected! Check your assignments.");
            return;
        }

        // Get a random position within the circular area
        Vector2 randomPosition = GetRandomPosition();

        // Instantiate enemy
        GameObject enemy = Instantiate(selectedEnemyPrefab, randomPosition, Quaternion.identity);
        currentEnemyCount++;

        // Subscribe to enemy death event
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.OnDeath += EnemyDied;
        }
    }

    /// <summary>
    /// Selects an enemy prefab based on weighted spawn rates
    /// Uses cumulative distribution method
    /// </summary>
    private GameObject SelectEnemyByWeight()
    {
        // Generate random value between 0 and 100
        float randomValue = Random.Range(0f, 100f);


        float cumulativeRate = 0f;

        // Check Level 1
        cumulativeRate += spawnRate_Level1;
        if (randomValue < cumulativeRate)
        {
            if (enemyLevel1Prefab != null)
                return enemyLevel1Prefab;
        }

        // Check Level 2
        cumulativeRate += spawnRate_Level2;
        if (randomValue < cumulativeRate)
        {
            if (enemyLevel2Prefab != null)
                return enemyLevel2Prefab;
        }

        // Level 3 (remaining probability)
        if (enemyLevel3Prefab != null)
            return enemyLevel3Prefab;

        // Fallback to Level 1 if nothing else available
        return enemyLevel1Prefab;
    }

    Vector2 GetRandomPosition()
    {
        float angle = Random.Range(0f, 2f * Mathf.PI);
        float distance = Random.Range(0f, spawnRadius);
        float x = transform.position.x + Mathf.Cos(angle) * distance;
        float y = transform.position.y + Mathf.Sin(angle) * distance;
        return new Vector2(x, y);
    }

    void EnemyDied()
    {
        currentEnemyCount--;
    }

    #region IDamageable Interface Implementation

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (!isFlashing)
        {
            StartCoroutine(FlashWhite());
        }

        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

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
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        CancelInvoke(nameof(SpawnEnemy));

        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        OnDeath?.Invoke();

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (!isFlashing)
        {
            StartCoroutine(FlashWhite());
        }

        yield return new WaitForSeconds(1f);

        Destroy(gameObject);
    }

    #endregion

    #region Visual Effects

    private IEnumerator FlashWhite()
    {
        if (spriteRenderer == null || isFlashing) yield break;

        isFlashing = true;

        spriteRenderer.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

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

    /// <summary>
    /// Set the dungeon level and recalculate spawn rates
    /// Call this from RoguelikeManager when starting a new dungeon
    /// </summary>
    public void SetDungeonLevel(int dungeonLevel)
    {
        currentDungeonLevel = dungeonLevel;
        CalculateSpawnRates(dungeonLevel);

        Debug.Log($"[EnemySpawner] Dungeon level set to {dungeonLevel}");
    }

    /// <summary>
    /// Adjusts spawn difficulty based on current level
    /// Every 2 levels: +1 max enemy
    /// Every 3 levels: -0.5f spawn interval (faster spawning)
    /// Call this when a new level starts
    /// </summary>
    public void UpdateDifficultyForLevel(int currentLevel)
    {
        if (currentLevel < 1)
        {
            Debug.LogWarning($"Invalid level: {currentLevel}. Must be >= 1");
            currentLevel = 1;
        }

        int enemyIncrements = (currentLevel - 1) / 2;
        int spawnRateIncrements = (currentLevel - 1) / 3;

        int newMaxEnemies = maxEnemies + enemyIncrements;
        float newSpawnInterval = spawnInterval - (spawnRateIncrements * 0.5f);

        newMaxEnemies = Mathf.Max(newMaxEnemies, 1);
        newSpawnInterval = Mathf.Max(newSpawnInterval, 0.5f);

        SetMaxEnemies(newMaxEnemies);
        SetSpawnRate(newSpawnInterval);

        // Also update dungeon level for spawn rates
        SetDungeonLevel(currentLevel);

        Debug.Log($"[EnemySpawner] Difficulty updated for Level {currentLevel}:");
        Debug.Log($"  - Max Enemies: {newMaxEnemies}");
        Debug.Log($"  - Spawn Interval: {newSpawnInterval}s");
    }

    /// <summary>
    /// Get current spawn rates (for debugging/UI)
    /// </summary>
    public void GetCurrentSpawnRates(out float level1Rate, out float level2Rate, out float level3Rate)
    {
        level1Rate = spawnRate_Level1;
        level2Rate = spawnRate_Level2;
        level3Rate = spawnRate_Level3;
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        Gizmos.color = isDead ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        if (Application.isPlaying && maxHealth > 0)
        {
            Vector3 healthBarPos = transform.position + Vector3.up * 2f;
            float healthPercentage = GetHealthPercentage();

            Gizmos.color = Color.red;
            Gizmos.DrawLine(healthBarPos - Vector3.right * 1f, healthBarPos + Vector3.right * 1f);

            Gizmos.color = Color.green;
            Vector3 healthEnd = healthBarPos + Vector3.right * (healthPercentage * 2f - 1f);
            Gizmos.DrawLine(healthBarPos - Vector3.right * 1f, healthEnd);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius * 1.5f);
    }

    #endregion
}
