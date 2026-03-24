using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnableEnemy
{
    public GameObject prefab;
    public int budgetCost;
    public float spawnWeight;
    public int maxAllowed = 0; // 0 = infinite
    [HideInInspector] public int currentSpawned = 0;
}

public class EnemySpawner : MonoBehaviour, IDamageable
{
    [Header("Spawner Economy (The Piñata)")]
    public int startingBudget = 300;
    private int currentBudget;
    public float spawnInterval = 1.5f;
    private float nextSpawnTime;

    [Header("Enemy Roster (Set Weights & Costs)")]
    [Tooltip("Easy weight, low cost")] public SpawnableEnemy fodderLevel1;
    [Tooltip("Easy weight, low cost")] public SpawnableEnemy fodderLevel2;
    [Tooltip("Easy weight, low cost")] public SpawnableEnemy fodderLevel3;
    [Tooltip("Mid weight, mid cost")] public SpawnableEnemy rangedEnemy;

    [Header("Cthulhu Eye Settings")]
    [Tooltip("Low weight, high cost, max 1")] public SpawnableEnemy cthulhuEye;
    public float cthulhuExclusionRadius = 25f;
    private static HashSet<Vector3> globalCthulhuLocations = new HashSet<Vector3>();

    [Header("Coin Rewards")]
    public GameObject coinPrefab;
    [Tooltip("Coins = Budget Remaining * Multiplier")]
    public float coinRewardMultiplier = 0.5f;
    [Tooltip("Keep low so coins don't clip through walls")]
    public float coinDropForce = 2f;

    [Header("Health & Visuals")]
    public int maxHealth = 100;
    private int currentHealth;
    public float flashDuration = 0.15f;
    public Color flashColor = Color.white;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isFlashing = false;
    private bool isDead = false;

    [Header("Spawning Logistics")]
    public float spawnRadius = 5f;
    public LayerMask wallLayer;
    public bool requiresActivation = true;
    private bool isActivated = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip activationSound;
    public AudioClip damageSound;
    public AudioClip deathSound;

    public System.Action<int, int> OnHealthChanged;
    public System.Action OnDeath;
    public System.Action OnActivated;

    private void Start()
    {
        currentBudget = startingBudget;
        currentHealth = maxHealth;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        if (!requiresActivation) ActivateSpawner();
    }

    private void Update()
    {
        if (isDead || !isActivated) return;

        if (Time.time >= nextSpawnTime && currentBudget > 0)
        {
            AttemptSpawn();
        }

        // Auto-Destroy when empty
        if (currentBudget <= 0)
        {
            // Die normally, but because budget is 0, it drops 0 coins
            Die();
        }
    }

    public void ActivateSpawner()
    {
        if (isActivated || isDead) return;

        isActivated = true;
        if (audioSource && activationSound) audioSource.PlayOneShot(activationSound);
        if (spriteRenderer) StartCoroutine(ActivationFlash());

        OnActivated?.Invoke();
    }

    private void AttemptSpawn()
    {
        SpawnableEnemy chosenEnemy = PickEnemyByWeight();

        if (chosenEnemy != null)
        {
            // 1. Pay the cost
            currentBudget -= chosenEnemy.budgetCost;
            chosenEnemy.currentSpawned++;

            // 2. Special check for Cthulhu tracking
            if (chosenEnemy == cthulhuEye)
            {
                globalCthulhuLocations.Add(transform.position);
            }

            // 3. Find a safe spot and spawn
            Vector2 safePosition = GetValidSpawnPosition();
            Instantiate(chosenEnemy.prefab, safePosition, Quaternion.identity);

            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private SpawnableEnemy PickEnemyByWeight()
    {
        List<SpawnableEnemy> validEnemies = new List<SpawnableEnemy>();
        float totalWeight = 0f;

        // Put all enemies into an array to easily loop through them
        SpawnableEnemy[] allEnemies = { fodderLevel1, fodderLevel2, fodderLevel3, rangedEnemy, cthulhuEye };

        foreach (var enemy in allEnemies)
        {
            if (enemy.prefab == null) continue;

            bool canAfford = enemy.budgetCost <= currentBudget;
            bool underCap = enemy.maxAllowed == 0 || enemy.currentSpawned < enemy.maxAllowed;

            // Special Cthulhu Check
            bool isCthulhuSafe = true;
            if (enemy == cthulhuEye) isCthulhuSafe = !IsCthulhuTooClose();

            if (canAfford && underCap && isCthulhuSafe)
            {
                validEnemies.Add(enemy);
                totalWeight += enemy.spawnWeight;
            }
        }

        if (validEnemies.Count == 0) return null;

        // Weighted Random Selection
        float randomVal = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var enemy in validEnemies)
        {
            cumulative += enemy.spawnWeight;
            if (randomVal <= cumulative) return enemy;
        }

        return null;
    }

    #region Physics & Wall Detection
    private Vector2 GetValidSpawnPosition()
    {
        // Pick a random direction
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float distance = Random.Range(1f, spawnRadius);

        // Raycast to check for walls
        RaycastHit2D hit = Physics2D.Raycast(transform.position, randomDir, distance, wallLayer);

        if (hit.collider != null)
        {
            // If we hit a wall, spawn slightly in front of it so enemy doesn't get stuck
            distance = Mathf.Max(1f, hit.distance - 0.5f);
        }

        return (Vector2)transform.position + (randomDir * distance);
    }

    private bool IsCthulhuTooClose()
    {
        foreach (Vector3 pos in globalCthulhuLocations)
        {
            if (Vector3.Distance(transform.position, pos) < cthulhuExclusionRadius)
                return true;
        }
        return false;
    }
    public static void ResetCthulhuEyeTracking() => globalCthulhuLocations.Clear();
    #endregion

    #region Piñata & Health System
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        if (!isFlashing) StartCoroutine(FlashWhite());
        if (audioSource && damageSound) audioSource.PlayOneShot(damageSound);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (audioSource && deathSound) audioSource.PlayOneShot(deathSound);
        OnDeath?.Invoke();

        // The Piñata Drop!
        DropCoins();

        StartCoroutine(DeathSequence());
    }

    private void DropCoins()
    {
        if (coinPrefab == null || currentBudget <= 0) return;

        int coinsToDrop = Mathf.RoundToInt(currentBudget * coinRewardMultiplier);

        for (int i = 0; i < coinsToDrop; i++)
        {
            GameObject coin = Instantiate(coinPrefab, transform.position, Quaternion.identity);

            // Apply a very gentle force so they pop out but don't clip through walls
            Rigidbody2D rb = coin.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 gentlePush = Random.insideUnitCircle * coinDropForce;
                rb.AddForce(gentlePush, ForceMode2D.Impulse);
            }
        }
    }

    private IEnumerator DeathSequence()
    {
        if (!isFlashing) StartCoroutine(FlashWhite());
        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }
    #endregion

    #region Visual Effects
    private IEnumerator ActivationFlash()
    {
        spriteRenderer.color = Color.yellow;
        yield return new WaitForSeconds(0.3f);
        spriteRenderer.color = originalColor;
    }

    private IEnumerator FlashWhite()
    {
        if (spriteRenderer == null) yield break;
        isFlashing = true;
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
        isFlashing = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        if (cthulhuEye != null && cthulhuEye.prefab != null)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, cthulhuExclusionRadius);
        }
    }
    #endregion

    // Interface required methods
    public bool IsDead() => isDead;
    public GameObject GetGameObject() => gameObject;

    #region Legacy API Bridge (Fixes for Managers and Activators)

    // --- Fixes for ActivateEnemySpawner.cs ---
    public bool RequiresActivation()
    {
        return requiresActivation;
    }

    public bool IsActivated()
    {
        return isActivated;
    }

    // --- Fixes for DungeonMapGenerator.cs ---
    // The generator tries to limit enemies. We add the variable back to satisfy the compiler.
    // (Though your Budget System and maxAllowed variables now do the real heavy lifting!)
    public int maxEnemies = 8;

    public void SetDungeonGenerator(MonoBehaviour generator)
    {
        // Intentionally left blank. 
        // You explicitly requested: "dont have the spawner be respawned".
        // So we accept the reference from the Generator to prevent errors, but we just ignore it.
    }

    // --- Fixes for RoguelikeManager.cs ---
    public void SetDungeonLevel(int level)
    {
        UpdateDifficultyForLevel(level);
    }

    public void UpdateDifficultyForLevel(int currentLevel)
    {
        // Translate the old "level" logic into your new Budget System!
        if (currentLevel <= 1) return;

        // Increase the Piñata budget for higher floors (e.g., +50 points per level)
        startingBudget += (currentLevel * 50);

        // If the spawner hasn't started spending yet, update its current wallet too
        if (currentBudget > 0 && currentHealth == maxHealth)
        {
            currentBudget = startingBudget;
        }

        // Make the spawn rate slightly faster on higher levels (caps at 0.5 seconds)
        spawnInterval = Mathf.Max(0.5f, spawnInterval - (currentLevel * 0.15f));

        Debug.Log($"[EnemySpawner] Upgraded for Level {currentLevel}. New Budget: {startingBudget}");
    }

    #endregion

}