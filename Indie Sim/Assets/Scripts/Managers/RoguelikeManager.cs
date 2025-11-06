using UnityEngine;

/// <summary>
/// RoguelikeManager handles dungeon progression and difficulty scaling
/// - Generates dungeons with increasing size based on 50% chance after each clear
/// - Tracks dungeon clears and difficulty increments
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

   

    private void Start()
    {
        // Initialize the very first dungeon when game starts
        GenerateNewDungeon();
    }


   
    private int GetCurrentDungeonSize()
    {
        int totalSize = BASE_DUNGEON_SIZE + dungeonSizeIncrement;
        return totalSize;
    }



    public void GenerateNewDungeon()
    {
        int dungeonSize = GetCurrentDungeonSize();

        Debug.Log($"[RoguelikeManager] Generating dungeon #{dungeonsClearedCount + 1}");
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

        // Disable all enemies by tag
        GameObject[] enemySpawners = GameObject.FindGameObjectsWithTag("EnemySpawner");
        foreach (GameObject enemyspawner in enemySpawners)
        {
            Destroy(enemyspawner);
        }
        Debug.Log($"[RoguelikeManager] Disabled {enemies.Length} enemies");

    }

    /// <summary>
    /// Call this when player closes the store and continues
    /// </summary>
    public void ContinueDungeon()
    {
        // Unpause the game
        Time.timeScale = 1f;

        Debug.Log("Continuing dungeon...");

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

        

        GenerateNewDungeon();
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
}
