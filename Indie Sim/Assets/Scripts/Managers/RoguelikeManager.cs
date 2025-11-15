using UnityEngine;

/// <summary>
/// RoguelikeManager handles dungeon progression and difficulty scaling
/// - Generates dungeons with increasing size based on 50% chance after each clear
/// - Tracks dungeon clears and difficulty increments
/// - Tracks level progression for difficulty scaling
/// - Resets player position and cleans up completed dungeons
/// </summary>
public class RoguelikeManager : MonoBehaviour
{
    [Header("Store Reference")]
    public StoreManager storeManager;

    [SerializeField] private DungeonMapGenerator dungeonGenerator;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Teleporter teleporter;

    // ===== ROGUELIKE PROGRESSION VARIABLES =====
    private int dungeonsClearedCount = 0;           // Tracks total dungeons completed
    private int dungeonSizeIncrement = 0;           // Tracks difficulty progression (each increment = +1 node)
    private const int BASE_DUNGEON_SIZE = 5;        // All dungeons start at 5 nodes
    private const float SIZE_INCREASE_CHANCE = 0.5f; // 50% chance to increase size after each clear

    // ✅ NEW: Level tracking for difficulty scaling
    private int currentLevel = 1;                   // Current level (starts at 1, increases each dungeon)

    private void Start()
    {
        // Initialize the very first dungeon when game starts
        GenerateNewDungeon();

        // ✅ NEW: Update spawner difficulty for level 1
        UpdateSpawnerDifficulty();
    }

    private int GetCurrentDungeonSize()
    {
        int totalSize = BASE_DUNGEON_SIZE + dungeonSizeIncrement;
        return totalSize;
    }

    public void GenerateNewDungeon()
    {
        int dungeonSize = GetCurrentDungeonSize();

        Debug.Log($"[RoguelikeManager] Generating dungeon #{dungeonsClearedCount + 1} (Level {currentLevel})");
        Debug.Log($"[RoguelikeManager] Dungeon Size: {dungeonSize} nodes (Base: {BASE_DUNGEON_SIZE} + Increments: {dungeonSizeIncrement})");

        // Just call it - don't try to capture return value
        dungeonGenerator.GenerateNewMap(dungeonSize);

        Debug.Log($"[RoguelikeManager] Dungeon generation complete!");
    }

    public void CompleteDungeon()
    {
        // Increment cleared count
        dungeonsClearedCount++;
        Debug.Log($"[RoguelikeManager] Dungeon #{dungeonsClearedCount} completed!");

        ClearCurrentDungeon();

        // Open the store
        if (storeManager != null)
        {
            storeManager.OpenStore();
        }
        else
        {
            Debug.LogError("StoreManager not assigned to RoguelikeManager!");
        }
    }

    /// <summary>
    /// Disables all enemies when dungeon is completed
    /// </summary>
    private void ClearCurrentDungeon()
    {
        Debug.Log("[RoguelikeManager] Disabling all enemies...");

        // Disable all enemies by tag
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }
        Debug.Log($"[RoguelikeManager] Disabled {enemies.Length} enemies");

        // Disable all enemy spawners by tag
        GameObject[] enemySpawners = GameObject.FindGameObjectsWithTag("EnemySpawner");
        foreach (GameObject enemyspawner in enemySpawners)
        {
            Destroy(enemyspawner);
        }
        Debug.Log($"[RoguelikeManager] Disabled {enemySpawners.Length} spawners");
    }

    /// <summary>
    /// Call this when player closes the store and continues
    /// </summary>
    public void ContinueDungeon()
    {
        // Unpause the game
        Time.timeScale = 1f;

        Debug.Log("Continuing dungeon...");

        // ✅ NEW: Increment level before generating next dungeon
        currentLevel++;
        Debug.Log($"[RoguelikeManager] 📈 Level increased to {currentLevel}!");

        // Roll 50/50 chance to increase difficulty
        float randomRoll = Random.value;

        if (randomRoll <= SIZE_INCREASE_CHANCE)
        {
            dungeonSizeIncrement++;
            Debug.Log($"[RoguelikeManager] 🎲 SIZE INCREASED! New difficulty level: {dungeonSizeIncrement}");
        }
        else
        {
            Debug.Log($"[RoguelikeManager] Size stays same for next dungeon");
        }

        // Reset player position to origin (0, 0, 0)
        playerTransform.position = Vector3.zero;
        Debug.Log($"[RoguelikeManager] Player repositioned to (0, 0, 0)");

        // Clear enemies and generate next dungeon
        Debug.Log($"[RoguelikeManager] Old dungeon cleared (tilemaps overwritten)");
        GenerateNewDungeon();

        // ✅ NEW: Update spawner difficulty for new level
        UpdateSpawnerDifficulty();
    }

    // ✅ NEW: Simplified method that uses currentLevel automatically
    /// <summary>
    /// Updates all enemy spawners to match the current level difficulty
    /// Automatically uses the internal currentLevel counter
    /// </summary>
    
    private void UpdateSpawnerDifficulty()
    {
        // Find all enemy spawners in the scene
        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>();

        foreach (EnemySpawner spawner in spawners)
        {
            // This now updates both spawn speed AND enemy type distribution
            spawner.UpdateDifficultyForLevel(currentLevel);
        }

        Debug.Log($"[RoguelikeManager] ✅ All {spawners.Length} spawners updated for Level {currentLevel}");
    }

    // ✅ KEPT: Public method in case you need manual control
    /// <summary>
    /// Manually update spawner difficulty for a specific level
    /// (Usually not needed - UpdateSpawnerDifficulty() is called automatically)
    /// </summary>
    public void OnLevelStart(int levelNumber)
    {
        // Find all enemy spawners in the scene
        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>();

        foreach (EnemySpawner spawner in spawners)
        {
            spawner.UpdateDifficultyForLevel(levelNumber);
        }

        Debug.Log($"All spawners updated for Level {levelNumber}");
    }

    [ContextMenu("DEBUG - Skip Current Dungeon")]
    public void DEBUG_SkipDungeon()
    {
        Debug.Log("[RoguelikeManager] ⏭️  DEBUG: Skipping dungeon!");
        CompleteDungeon();
    }

    [ContextMenu("DEBUG - Print Progression Stats")]
    public void DEBUG_PrintStats()
    {
        Debug.Log($"");
        Debug.Log($"╔════════════ ROGUELIKE PROGRESSION STATS ════════════╗");
        Debug.Log($"║ Current Level: {currentLevel}"); // ✅ NEW
        Debug.Log($"║ Dungeons Cleared: {dungeonsClearedCount}");
        Debug.Log($"║ Difficulty Increments: {dungeonSizeIncrement}");
        Debug.Log($"║ Current Dungeon Size: {GetCurrentDungeonSize()} nodes");
        Debug.Log($"╚═══════════════════════════════════════════════════╝");
        Debug.Log($"");
    }

    [ContextMenu("DEBUG - Reset All Progression")]
    public void DEBUG_ResetProgression()
    {
        Debug.Log("[RoguelikeManager] 🔄 DEBUG: Resetting all progression!");
        dungeonsClearedCount = 0;
        dungeonSizeIncrement = 0;
        currentLevel = 1; // ✅ NEW: Reset level to 1

        GenerateNewDungeon();
        UpdateSpawnerDifficulty(); // ✅ NEW: Update spawners to level 1
        DEBUG_PrintStats();
    }

    // ===== PUBLIC GETTERS (For other scripts to read your progression) =====

    public int GetDungeonsClearedCount()
    {
        return dungeonsClearedCount;
    }

    public int GetDungeonSizeIncrement()
    {
        return dungeonSizeIncrement;
    }

    public int GetCurrentDungeonNodeCount()
    {
        return GetCurrentDungeonSize();
    }

    // ✅ NEW: Getter for current level
    /// <summary>
    /// Returns the current level number (starts at 1)
    /// </summary>
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
}
