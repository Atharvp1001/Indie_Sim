using System.Collections;
using System.Collections.Generic;
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
    public float spawnInterval = 1.5f; // Time between spawns
    public int maxEnemies = 8; // Max number of enemies at a time

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

    [Header("Wall Detection")]
    [SerializeField] private LayerMask wallLayer; // Assign your wall layer in inspector

    [Header("Respawn Settings")]
    [SerializeField] private bool canRespawn = true;
    [SerializeField] private float respawnDelay = 10f; // Time before respawning
    [SerializeField] private float respawnExclusionRadius = 12f; // Area where new spawner can't spawn after this one dies

    [Header("Activation Settings")]
    [SerializeField] private bool requiresActivation = true; // Does this spawner need player activation?
    [SerializeField] private bool isActivated = false; // Has it been activated?
    [SerializeField] private AudioClip activationSound; // Sound when activated

    [Header("Burst Spawn Settings")]
    [SerializeField] private bool useBurstMode = true; // Use burst spawning instead of continuous
    [SerializeField] private int minEnemiesPerBurst = 5;
    [SerializeField] private int maxEnemiesPerBurst = 8;
    [SerializeField] private float burstCooldown = 10f; // Time between bursts
    [SerializeField] private int maxActiveBursts = 3; // Max number of bursts
    [SerializeField] private int maxTotalEnemies = 20; // Total enemies this spawner can create

    [Header("Special Enemy - Cthulhu Eye")]
    [SerializeField] private GameObject cthulhuEyePrefab;
    [SerializeField] private bool canSpawnCthulhuEye = true; // Toggle per spawner
    [SerializeField] [Range(0f, 1f)] private float cthulhuEyeSpawnChance = 0.3f; // 30% chance
    [SerializeField] private float cthulhuEyeSpawnRadius = 25f; // Exclusion radius
    [SerializeField] private int minDungeonLevelForCthulhuEye = 2; // Only spawn after level 1

    private bool hasCthulhuEyeSpawned = false; // Track if this spawner has spawned one
    private static HashSet<Vector3> cthulhuEyeSpawnLocations = new HashSet<Vector3>(); // Global tracking
    private int burstsCompleted = 0;
    private int totalEnemiesSpawned = 0;



    private DungeonMapGenerator dungeonGenerator;
    // Components
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isFlashing = false;
    private bool isDead = false;

    // Spawning tracking
    private int currentEnemyCount = 0;

    // Spawn rate cache (calculated once per dungeon)
    private float spawnRate_Level1 = 60f;
    private float spawnRate_Level2 = 25f;
    private float spawnRate_Level3 = 5f;
    
    //For Respawn Settings.
    public float GetRespawnExclusionRadius() => respawnExclusionRadius;
    public bool CanRespawn() => canRespawn;
    // Events
    public System.Action<int, int> OnHealthChanged;
    public System.Action OnDeath;
    public System.Action OnActivated;

    void Start()
    {
        Debug.Log($"[EnemySpawner] ========== SPAWNER START ==========");
        Debug.Log($"[EnemySpawner] Position: {transform.position}");
        Debug.Log($"[EnemySpawner] requiresActivation = {requiresActivation}");
        Debug.Log($"[EnemySpawner] isActivated = {isActivated}");
        Debug.Log($"[EnemySpawner] useBurstMode = {useBurstMode}");

        InitializeSpawner();

        // Calculate spawn rates for current dungeon
        CalculateSpawnRates(currentDungeonLevel);

        // Try to spawn Cthulhu Eye (if conditions met)
        TrySpawnCthulhuEye();

        // IMPORTANT: Only start spawning if activation is NOT required
        if (!requiresActivation)
        {
            Debug.Log($"[EnemySpawner] ⚠️ SPAWNING IMMEDIATELY (requiresActivation = false)");
            StartSpawning();
        }
        else if (isActivated)
        {
            Debug.Log($"[EnemySpawner] ⚠️ SPAWNING IMMEDIATELY (isActivated = true)");
            StartSpawning();
        }
        else
        {
            Debug.Log($"[EnemySpawner] ✅ WAITING for player activation...");
        }

        Debug.Log($"[EnemySpawner] =====================================");
    }



    public void ActivateSpawner()
    {
        Debug.Log($"[EnemySpawner] ========== ACTIVATE CALLED ==========");
        Debug.Log($"[EnemySpawner] Position: {transform.position}");
        Debug.Log($"[EnemySpawner] isActivated (before): {isActivated}");
        Debug.Log($"[EnemySpawner] isDead: {isDead}");

        if (isActivated || isDead)
        {
            Debug.Log($"[EnemySpawner] ❌ ACTIVATION BLOCKED (already activated or dead)");
            return;
        }

        isActivated = true;
        Debug.Log($"[EnemySpawner] ✅ ACTIVATING NOW!");

        // Play activation sound
        if (audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound);
        }

        // Visual feedback
        if (spriteRenderer != null)
        {
            StartCoroutine(ActivationFlash());
        }

        OnActivated?.Invoke();

        // Start spawning
        StartSpawning();
        Debug.Log($"[EnemySpawner] =====================================");
    }

    /// <summary>
    /// Visual feedback when spawner activates
    /// </summary>
    private IEnumerator ActivationFlash()
    {
        Color activationColor = Color.yellow;
        Color originalCol = spriteRenderer.color;
        spriteRenderer.color = activationColor;
        yield return new WaitForSeconds(0.3f);
        spriteRenderer.color = originalCol;
    }

    private void StartSpawning()
    {
        Debug.Log($"[EnemySpawner] ========== START SPAWNING ==========");
        Debug.Log($"[EnemySpawner] useBurstMode = {useBurstMode}");

        if (useBurstMode)
        {
            Debug.Log($"[EnemySpawner] Starting BURST MODE");
            // Spawn first burst immediately
            SpawnBurst();

            // Start burst cycle
            StartCoroutine(BurstCycle());
        }
        else
        {
            Debug.Log($"[EnemySpawner] Starting CONTINUOUS MODE");
            // Original continuous spawning
            InvokeRepeating(nameof(SpawnEnemy), spawnInterval, spawnInterval);
        }
        Debug.Log($"[EnemySpawner] =====================================");
    }


    /// <summary>
    /// Coroutine that handles burst spawning cycle
    /// </summary>
    private IEnumerator BurstCycle()
    {
        while (burstsCompleted < maxActiveBursts && !isDead)
        {
            // Wait for cooldown
            yield return new WaitForSeconds(burstCooldown);

            // Check if we should spawn another burst
            if (totalEnemiesSpawned >= maxTotalEnemies)
            {
                Debug.Log($"[EnemySpawner] Max total enemies reached ({maxTotalEnemies})");
                break;
            }

            // Spawn next burst
            SpawnBurst();
        }

        Debug.Log($"[EnemySpawner] Completed all {burstsCompleted} bursts");
    }

    /// <summary>
    /// Spawns a burst of enemies (between min and max)
    /// </summary>
    private void SpawnBurst()
    {
        if (isDead) return;

        int enemiesToSpawn = Random.Range(minEnemiesPerBurst, maxEnemiesPerBurst + 1);

        // Don't exceed max total enemies
        if (useBurstMode)
        {
            int remainingCapacity = maxTotalEnemies - totalEnemiesSpawned;
            enemiesToSpawn = Mathf.Min(enemiesToSpawn, remainingCapacity);
        }

        if (enemiesToSpawn <= 0)
        {
            return;
        }

        Debug.Log($"[EnemySpawner] Spawning burst of {enemiesToSpawn} enemies");

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
        }

        burstsCompleted++;
    }

    /// <summary>
    /// Check if spawner is activated (for PlayerSpawnerActivator to query)
    /// </summary>
    public bool IsActivated()
    {
        return isActivated;
    }

    /// <summary>
    /// Check if spawner requires activation
    /// </summary>
    public bool RequiresActivation()
    {
        return requiresActivation;
    }


    public void SetDungeonGenerator(DungeonMapGenerator generator)
    {
        dungeonGenerator = generator;
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

    void SpawnEnemy()
    {
        if (isDead) return;

        // In burst mode, check total enemy cap
        if (useBurstMode && totalEnemiesSpawned >= maxTotalEnemies)
        {
            return;
        }

        // In continuous mode, check current count
        if (!useBurstMode && currentEnemyCount >= maxEnemies)
        {
            return;
        }

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

        if (useBurstMode)
        {
            totalEnemiesSpawned++;
        }

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

    /// <summary>
    /// Gets valid spawn directions by checking which sides don't have walls
    /// Uses multiple raycasts per direction for better detection
    /// </summary>
    private List<Vector2> GetValidSpawnDirections()
    {
        List<Vector2> validDirections = new List<Vector2>();
        
        // All 4 cardinal directions
        Vector2[] cardinalDirections = new Vector2[]
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };

        // Check each direction for walls with MULTIPLE raycasts
        foreach (var direction in cardinalDirections)
        {
            bool hasWall = false;
            
            // Cast multiple rays in slightly different positions to ensure we catch walls
            for (int i = -1; i <= 1; i++)
            {
                Vector2 perpendicular = new Vector2(-direction.y, direction.x); // Get perpendicular direction
                Vector2 offset = perpendicular * (i * 0.3f); // Offset by 0.3 units
                Vector2 startPos = (Vector2)transform.position + offset;
                
                // Raycast to check if there's a wall in this direction
                RaycastHit2D hit = Physics2D.Raycast(startPos, direction, spawnRadius + 1f, wallLayer);
                
                // Debug visualization (comment out after testing)
                Debug.DrawRay(startPos, direction * (spawnRadius + 1f), hit.collider != null ? Color.red : Color.green, 0.5f);
                
                if (hit.collider != null)
                {
                    hasWall = true;
                    break; // Found a wall in this direction, no need to check more
                }
            }
            
            // If no wall detected in any of the raycasts, this direction is valid
            if (!hasWall)
            {
                validDirections.Add(direction);
            }
        }

        // Fallback: if NO valid directions found, log warning and return all directions
        if (validDirections.Count == 0)
        {
            Debug.LogWarning($"No valid spawn directions found at {transform.position}! This spawner might be in a bad position. Allowing all directions.");
            return new List<Vector2>(cardinalDirections);
        }

        return validDirections;
    }

    Vector2 GetRandomPosition()
    {
        List<Vector2> validDirections = GetValidSpawnDirections();
        
        if (validDirections.Count == 0)
        {
            Debug.LogWarning("No valid spawn directions found! Spawning at spawner position.");
            return transform.position;
        }

        // Calculate the average direction away from walls (the "safest" direction)
        Vector2 primaryDirection = Vector2.zero;
        foreach (var dir in validDirections)
        {
            primaryDirection += dir;
        }
        primaryDirection = primaryDirection.normalized;

        // Define cone angle based on how many valid directions we have
        float coneAngle = validDirections.Count switch
        {
            1 => 60f,  // Narrow cone if only 1 valid direction
            2 => 90f,  // Medium cone if 2 valid directions
            3 => 120f, // Wide cone if 3 valid directions
            _ => 140f  // Very wide cone if all 4 directions valid
        };

        // Get the angle of the primary direction
        float primaryAngle = Mathf.Atan2(primaryDirection.y, primaryDirection.x) * Mathf.Rad2Deg;
        
        // Random angle within the cone
        float randomAngleOffset = Random.Range(-coneAngle / 2f, coneAngle / 2f);
        float finalAngle = (primaryAngle + randomAngleOffset) * Mathf.Deg2Rad;
        
        // Create final direction vector
        Vector2 finalDirection = new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));
        
        // Random distance within spawn radius
        float distance = Random.Range(1f, spawnRadius);
        
        Vector2 spawnPos = (Vector2)transform.position + finalDirection * distance;
        
        // Safety check: make sure spawn position doesn't have a wall
        RaycastHit2D wallCheck = Physics2D.Raycast(transform.position, finalDirection, distance, wallLayer);
        if (wallCheck.collider != null)
        {
            // Hit a wall, spawn closer (just before the wall)
            distance = Mathf.Max(1f, wallCheck.distance - 0.5f);
            spawnPos = (Vector2)transform.position + finalDirection * distance;
        }
        
        return spawnPos;
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

        // Stop all spawning
        CancelInvoke(nameof(SpawnEnemy));
        StopAllCoroutines();

        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        OnDeath?.Invoke();

        // Request respawn if enabled and generator is set
        if (canRespawn && dungeonGenerator != null)
        {
            dungeonGenerator.RequestSpawnerRespawn(transform.position, respawnExclusionRadius, respawnDelay);
        }

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

        // Only restart spawning if already active and not in burst mode
        if (isActivated && !isDead && !useBurstMode)
        {
            CancelInvoke(nameof(SpawnEnemy));
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

        // ===== IMPORTANT FIX: Don't start spawning if activation is required =====
        // Only update the settings, don't start the spawner

        // Update burst settings based on level
        if (useBurstMode)
        {
            minEnemiesPerBurst = 5 + (currentLevel - 1) / 2;
            maxEnemiesPerBurst = 8 + (currentLevel - 1) / 2;
            burstCooldown = Mathf.Max(5f, 10f - (currentLevel - 1) * 0.5f);
            maxTotalEnemies = 20 + (currentLevel - 1) * 3;
        }
        else
        {
            // Continuous mode settings
            int enemyIncrements = (currentLevel - 1) / 2;
            int spawnRateIncrements = (currentLevel - 1) / 3;

            int newMaxEnemies = maxEnemies + enemyIncrements;
            float newSpawnInterval = spawnInterval - (spawnRateIncrements * 0.5f);

            newMaxEnemies = Mathf.Max(newMaxEnemies, 1);
            newSpawnInterval = Mathf.Max(newSpawnInterval, 0.5f);

            SetMaxEnemies(newMaxEnemies);

            // DON'T call SetSpawnRate() as it starts InvokeRepeating
            spawnInterval = newSpawnInterval;
        }

        // Also update dungeon level for spawn rates
        SetDungeonLevel(currentLevel);

        Debug.Log($"[EnemySpawner] Difficulty updated for Level {currentLevel}:");
        if (useBurstMode)
        {
            Debug.Log($"  - Burst size: {minEnemiesPerBurst}-{maxEnemiesPerBurst}");
            Debug.Log($"  - Burst cooldown: {burstCooldown}s");
            Debug.Log($"  - Max total enemies: {maxTotalEnemies}");
        }
        else
        {
            Debug.Log($"  - Max Enemies: {maxEnemies}");
            Debug.Log($"  - Spawn Interval: {spawnInterval}s");
        }
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

    /// <summary>
    /// Attempts to spawn a Cthulhu Eye if all conditions are met
    /// </summary>
    private void TrySpawnCthulhuEye()
    {
        // Check if we can spawn
        if (!canSpawnCthulhuEye)
        {
            Debug.Log("[EnemySpawner] Cthulhu Eye spawning disabled on this spawner");
            return;
        }

        if (cthulhuEyePrefab == null)
        {
            Debug.LogWarning("[EnemySpawner] Cthulhu Eye prefab not assigned!");
            return;
        }

        if (hasCthulhuEyeSpawned)
        {
            Debug.Log("[EnemySpawner] This spawner already spawned a Cthulhu Eye");
            return;
        }

        // Check dungeon level requirement
        if (currentDungeonLevel < minDungeonLevelForCthulhuEye)
        {
            Debug.Log($"[EnemySpawner] Dungeon level {currentDungeonLevel} < {minDungeonLevelForCthulhuEye} - no Cthulhu Eye");
            return;
        }

        // Check if another Cthulhu Eye is too close
        if (IsCthulhuEyeTooClose())
        {
            Debug.Log($"[EnemySpawner] Another Cthulhu Eye too close (within {cthulhuEyeSpawnRadius}m)");
            return;
        }

        // Random chance
        if (Random.value > cthulhuEyeSpawnChance)
        {
            Debug.Log($"[EnemySpawner] Random roll failed ({cthulhuEyeSpawnChance * 100}% chance)");
            return;
        }

    // ALL CHECKS PASSED - SPAWN IT!
    SpawnCthulhuEye();
    }

    /// <summary>
    /// Checks if any Cthulhu Eye spawn location is within the exclusion radius
    /// </summary>
    private bool IsCthulhuEyeTooClose()
    {
        foreach (Vector3 spawnPos in cthulhuEyeSpawnLocations)
        {
            float distance = Vector3.Distance(transform.position, spawnPos);
            if (distance < cthulhuEyeSpawnRadius)
            {
                Debug.Log($"[EnemySpawner] Found nearby Cthulhu Eye at distance {distance}m");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Spawns a Cthulhu Eye at this spawner's position
    /// </summary>
    private void SpawnCthulhuEye()
    {
        GameObject eye = Instantiate(cthulhuEyePrefab, transform.position, Quaternion.identity);
        
        // Mark this spawner as having spawned one
        hasCthulhuEyeSpawned = true;
        
        // Add to global tracking (PERMANENT - never removed)
        cthulhuEyeSpawnLocations.Add(transform.position);
        
        Debug.Log($"[EnemySpawner] ✅ SPAWNED CTHULHU EYE at {transform.position}");
        Debug.Log($"[EnemySpawner] Total Cthulhu Eye spawn locations: {cthulhuEyeSpawnLocations.Count}");
    }

    /// <summary>
    /// Call this when a new dungeon is generated to clear Cthulhu Eye tracking
    /// </summary>
    public static void ResetCthulhuEyeTracking()
    {
        cthulhuEyeSpawnLocations.Clear();
        Debug.Log("[EnemySpawner] Cthulhu Eye tracking reset for new dungeon");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        // Show respawn exclusion radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, respawnExclusionRadius);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius * 1.5f);

        // Visualize spawn cone (only in play mode)
        if (Application.isPlaying)
        {
            List<Vector2> validDirs = GetValidSpawnDirections();
            
            if (validDirs.Count > 0)
            {
                // Calculate primary direction
                Vector2 primaryDirection = Vector2.zero;
                foreach (var dir in validDirs)
                {
                    primaryDirection += dir;
                    
                    // Draw individual valid directions
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(transform.position, dir * spawnRadius);
                }
                primaryDirection = primaryDirection.normalized;
                
                // Determine cone angle
                float coneAngle = validDirs.Count switch
                {
                    1 => 60f,
                    2 => 90f,
                    3 => 120f,
                    _ => 140f
                };
                
                // Draw the spawn cone
                float primaryAngle = Mathf.Atan2(primaryDirection.y, primaryDirection.x) * Mathf.Rad2Deg;
                
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green
                
                // Draw cone edges
                float leftEdgeAngle = (primaryAngle - coneAngle / 2f) * Mathf.Deg2Rad;
                float rightEdgeAngle = (primaryAngle + coneAngle / 2f) * Mathf.Deg2Rad;
                
                Vector3 leftEdge = new Vector3(Mathf.Cos(leftEdgeAngle), Mathf.Sin(leftEdgeAngle), 0) * spawnRadius;
                Vector3 rightEdge = new Vector3(Mathf.Cos(rightEdgeAngle), Mathf.Sin(rightEdgeAngle), 0) * spawnRadius;
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, leftEdge);
                Gizmos.DrawRay(transform.position, rightEdge);
                
                // Draw center of cone
                Gizmos.color = Color.white;
                Gizmos.DrawRay(transform.position, primaryDirection * spawnRadius);
            }
            
            // Visualize blocked directions
            Vector2[] allDirs = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            foreach (var dir in allDirs)
            {
                if (!validDirs.Contains(dir))
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(transform.position, dir * spawnRadius);
                }
            }
        }
            if (canSpawnCthulhuEye && cthulhuEyePrefab != null)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.2f); // Magenta
            Gizmos.DrawWireSphere(transform.position, cthulhuEyeSpawnRadius);
            
            // Show if this spawner has already spawned one
            if (Application.isPlaying && hasCthulhuEyeSpawned)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(transform.position + Vector3.up * 3f, 0.5f);
            }
        }
        
        // Visualize all Cthulhu Eye spawn locations
        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            foreach (Vector3 pos in cthulhuEyeSpawnLocations)
            {
                Gizmos.DrawWireSphere(pos, cthulhuEyeSpawnRadius);
            }
        }
    }
    #endregion
}
