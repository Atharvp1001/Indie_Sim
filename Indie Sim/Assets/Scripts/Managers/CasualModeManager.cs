using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class CasualGameModeManager : MonoBehaviour
{
    [Header("Stage Configurations")]
    [Tooltip("All stage configurations - will be overridden by menu selection")]
    public StageConfigSO[] stageConfigs;

    [Header("Dungeon Generator Reference")]
    [Tooltip("Reference to your dungeon generator")]
    public DungeonMapGenerator dungeonGenerator;

    [Header("Base Map Parameters")]
    [Tooltip("Base parameters to use as template")]
    public MapParameters baseMapParameters;

    [Header("Scene Settings")]
    [Tooltip("Name of the store scene")]
    public string storeSceneName = "StoreScene";

    [Tooltip("Name of the casual mode menu scene")]
    public string menuSceneName = "CasualModeMenuScene";

    [Header("Progression Tracking")]
    private int currentStageIndex = 0;
    private int currentLevelIndex = 0;
    private int totalLevelsCompleted = 0;

    [Header("Coin Rewards")]
    [Tooltip("Base coins awarded per level completion")]
    public int baseCoinReward = 50;


    [Header("Events")]
    public UnityEvent<int> OnLevelCompleted;
    public UnityEvent<int> OnStageCompleted;
    public UnityEvent OnAllStagesCompleted;

    [Header("Debug Settings")]
    public bool enableDebugKeys = true;

    // Properties for easy access
    public int CurrentStageNumber => currentStageIndex + 1;
    public int CurrentLevelNumber => currentLevelIndex + 1;
    public int TotalLevelsCompleted => totalLevelsCompleted;
    public StageConfigSO CurrentStageConfig => stageConfigs[currentStageIndex];

    void Start()
    {
        // Auto-create PersistentDataManager if it doesn't exist
        if (PersistentDataManager.Instance == null)
        {
            GameObject managerObj = new GameObject("PersistentDataManager");
            managerObj.AddComponent<PersistentDataManager>();
            Debug.LogWarning("PersistentDataManager created automatically. Did you skip the menu?");
        }

        // Load the selected stage configuration from PersistentDataManager
        if (PersistentDataManager.Instance != null && PersistentDataManager.Instance.selectedStageConfig != null)
        {
            // Use only the selected stage
            stageConfigs = new StageConfigSO[] { PersistentDataManager.Instance.selectedStageConfig };
            currentStageIndex = 0;
            currentLevelIndex = 0;
            totalLevelsCompleted = 0;

            Debug.Log($"<color=cyan>Loaded Stage {stageConfigs[0].stageNumber} from menu selection</color>");
        }
        else
        {
            // Fallback: if no stage selected but stageConfigs is assigned, use the first one
            if (stageConfigs != null && stageConfigs.Length > 0)
            {
                Debug.LogWarning("No stage selected from menu - using first stage in array for testing");
            }
            else
            {
                Debug.LogError("No stage configurations available! Assign stages in Inspector or load from menu.");
                return;
            }
        }

        // Validate dungeon generator
        if (dungeonGenerator == null)
        {
            Debug.LogError("CasualGameModeManager: Dungeon generator not assigned!");
            return;
        }

        // Create base parameters if not assigned
        if (baseMapParameters == null)
        {
            Debug.LogWarning("Base Map Parameters not assigned. Creating default parameters.");
            baseMapParameters = new MapParameters();
        }

        Debug.Log($"Starting Casual Game Mode - Stage {stageConfigs[0].stageNumber}");
        PrintGameModeInfo();

        // Generate the first dungeon
        GenerateCurrentDungeon();
    }

    void Update()
    {
        // Debug controls
        if (enableDebugKeys)
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log("DEBUG: Force completing current level");
                CompleteCurrentLevel();
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log("DEBUG: Force completing current stage");
                ForceCompleteStage();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("DEBUG: Resetting progression");
                ResetProgression();
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                PrintCurrentStatus();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                Debug.Log("DEBUG: Regenerating current dungeon");
                GenerateCurrentDungeon();
            }
        }
    }

    /// <summary>
    /// Generates a dungeon based on the current stage and level
    /// </summary>
    public void GenerateCurrentDungeon()
    {
        StageConfigSO currentStage = stageConfigs[currentStageIndex];

        if (currentStage.dungeonConfigs == null || currentStage.dungeonConfigs.Length == 0)
        {
            Debug.LogError($"No dungeon configs set for Stage {CurrentStageNumber}!");
            return;
        }

        // Get MapParameters from the stage config
        MapParameters mapParams = currentStage.GetMapParameters(currentLevelIndex, baseMapParameters);

        Debug.Log($"<color=cyan>========== GENERATING DUNGEON ==========</color>");
        Debug.Log($"<color=yellow>Stage {CurrentStageNumber}, Level {CurrentLevelNumber}</color>");
        Debug.Log($"<color=green>Parameters: Main Artery Rooms={mapParams.minMainArteryRooms}-{mapParams.maxMainArteryRooms}</color>");
        Debug.Log($"<color=green>Spacing: {mapParams.mainRoomSpacing}, L-Turn Chance: {mapParams.chanceForLTurn}</color>");
        Debug.Log($"<color=cyan>========================================</color>");

        if (dungeonGenerator != null)
        {
            dungeonGenerator.GenerateNewMap(mapParams);
        }
        else
        {
            Debug.LogError("Dungeon generator reference is null!");
        }
    }

    /// <summary>
    /// Call this when a level is completed
    /// </summary>
    /// <summary>
    /// Call this when a level is completed
    /// </summary>
    public void CompleteCurrentLevel()
    {
        // FIRST LINE - Check for loop
        if (totalLevelsCompleted > 100)
        {
            Debug.LogError("LOOP DETECTED! Aborting!");
            return; // Stop the loop
        }
        Debug.Log($"<color=lime>✓ Level {CurrentLevelNumber} of Stage {CurrentStageNumber} COMPLETED!</color>");

        // Award coins
        if (PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.AddCoins(baseCoinReward);
        }

        currentLevelIndex++;
        totalLevelsCompleted++;

        // Save progress
        if (PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.SaveProgress(currentStageIndex, currentLevelIndex, totalLevelsCompleted);
        }

        OnLevelCompleted?.Invoke(totalLevelsCompleted);

        Debug.Log($"<color=orange>Progress: {GetProgressString()} ({GetProgressPercentage():F1}%)</color>");

        // Check if we've completed all levels in this stage
        if (currentLevelIndex >= stageConfigs[currentStageIndex].numberOfLevels)
        {
            CompleteCurrentStage();
        }
        else
        {
            // Load store scene between levels
            LoadStoreScene();
        }
    }

    /// <summary>
    /// Load the store scene
    /// </summary>
    private void LoadStoreScene()
    {
        Debug.Log("<color=cyan>Loading Store Scene...</color>");
        SceneManager.LoadScene(storeSceneName);
    }



    /// <summary>
    /// Called when a stage is completed
    /// </summary>
    private void CompleteCurrentStage()
    {
        Debug.Log($"<color=magenta>★★★ STAGE {CurrentStageNumber} COMPLETED! ★★★</color>");

        OnStageCompleted?.Invoke(CurrentStageNumber);

        // Save that this stage was completed (unlocks next stage)
        int completedStageNumber = stageConfigs[0].stageNumber;
        PlayerPrefs.SetInt("HighestCompletedStage", completedStageNumber);
        PlayerPrefs.Save();

        Debug.Log($"<color=yellow>Stage {completedStageNumber} marked as complete!</color>");

        // Check if there's a next stage available
        if (HasNextStage())
        {
            // Go to store before next stage
            LoadStoreSceneForStageTransition();
        }
        else
        {
            // No more stages - game complete!
            Debug.Log($"<color=yellow>╔══════════════════════════════════════╗</color>");
            Debug.Log($"<color=yellow>║   ALL STAGES COMPLETED! YOU WIN!    ║</color>");
            Debug.Log($"<color=yellow>╚══════════════════════════════════════╝</color>");
            OnAllStagesCompleted?.Invoke();

            // Return to menu after completion
            LoadCasualModeMenu();
        }
    }

    /// <summary>
    /// Check if there's a next stage after the current one
    /// </summary>
    private bool HasNextStage()
    {
        if (PersistentDataManager.Instance == null || PersistentDataManager.Instance.selectedStageConfig == null)
        {
            return false;
        }

        // Get all available stages from the menu manager
        int currentStageNum = PersistentDataManager.Instance.selectedStageConfig.stageNumber;
        int totalStages = GetTotalStagesInGame();

        return currentStageNum < totalStages;
    }

    /// <summary>
    /// Get total number of stages in the game
    /// </summary>
    private int GetTotalStagesInGame()
    {
        // This should match the number of stage configs you have
        // For now, we'll use PlayerPrefs or hardcode it
        // Better approach: Store this in PersistentDataManager
        return 3; // Change this to match your total stage count
    }

    /// <summary>
    /// Load store scene with flag for stage transition
    /// </summary>
    private void LoadStoreSceneForStageTransition()
    {
        if (PersistentDataManager.Instance != null)
        {
            // Set a flag so store knows this is a stage transition
            PersistentDataManager.Instance.currentSession.isStageTransition = true;
        }

        Debug.Log("<color=cyan>Loading Store for stage transition...</color>");
        SceneManager.LoadScene(storeSceneName);
    }

    /// <summary>
    /// Load casual mode menu
    /// </summary>
    private void LoadCasualModeMenu()
    {
        Debug.Log("<color=cyan>Returning to Casual Mode Menu...</color>");
        SceneManager.LoadScene(menuSceneName);
    }



    /// <summary>
    /// Called when returning from store - continue to next level
    /// </summary>
    public void ContinueAfterStore()
    {
        Debug.Log("Continuing after store...");
        GenerateCurrentDungeon();
    }

    /// <summary>
    /// Get progress as a percentage (0-100)
    /// </summary>
    public float GetProgressPercentage()
    {
        int totalLevelsInGame = 0;
        foreach (var stage in stageConfigs)
        {
            totalLevelsInGame += stage.numberOfLevels;
        }

        if (totalLevelsInGame == 0) return 0;
        return (float)totalLevelsCompleted / totalLevelsInGame * 100f;
    }

    /// <summary>
    /// Get the current stage's enemy spawn multiplier
    /// </summary>
    public float GetCurrentEnemySpawnMultiplier()
    {
        if (currentStageIndex < stageConfigs.Length)
        {
            return stageConfigs[currentStageIndex].enemySpawnMultiplier;
        }
        return 1f;
    }

    /// <summary>
    /// Reset progression (for debugging or restarting)
    /// </summary>
    public void ResetProgression()
    {
        currentStageIndex = 0;
        currentLevelIndex = 0;
        totalLevelsCompleted = 0;

        Debug.Log("<color=red>Progression RESET - Starting from Stage 1, Level 1</color>");
        GenerateCurrentDungeon();
    }

    /// <summary>
    /// Get formatted progress string
    /// </summary>
    public string GetProgressString()
    {
        return $"Stage {CurrentStageNumber} - Level {CurrentLevelNumber}/{stageConfigs[currentStageIndex].numberOfLevels}";
    }

    // ============ DEBUG METHODS ============

    [ContextMenu("Print Game Mode Info")]
    public void PrintGameModeInfo()
    {
        Debug.Log("========== GAME MODE CONFIGURATION ==========");
        Debug.Log($"Total Stages: {stageConfigs.Length}");

        int totalLevels = 0;
        for (int i = 0; i < stageConfigs.Length; i++)
        {
            StageConfigSO stage = stageConfigs[i];
            totalLevels += stage.numberOfLevels;

            Debug.Log($"\n--- Stage {stage.stageNumber} ---");
            Debug.Log($"  Levels: {stage.numberOfLevels}");
            Debug.Log($"  Dungeon Configs: {stage.dungeonConfigs.Length}");
            Debug.Log($"  Enemy Multiplier: {stage.enemySpawnMultiplier}x");

            for (int j = 0; j < stage.dungeonConfigs.Length; j++)
            {
                var config = stage.dungeonConfigs[j];
                Debug.Log($"    Config {j}: Rooms {config.minMainArteryRooms}-{config.maxMainArteryRooms}");
            }
        }

        Debug.Log($"\nTotal Levels in Game: {totalLevels}");
        Debug.Log("=============================================");
    }

    [ContextMenu("Print Current Status")]
    [ContextMenu("Print Current Status")]
    public void PrintCurrentStatus()
    {
        Debug.Log("========== CURRENT STATUS ==========");
        Debug.Log($"Stage: {CurrentStageNumber}/{stageConfigs.Length}");
        Debug.Log($"Level: {CurrentLevelNumber}/{stageConfigs[currentStageIndex].numberOfLevels}");
        Debug.Log($"Total Completed: {totalLevelsCompleted}");
        Debug.Log($"Progress: {GetProgressPercentage():F1}%");
        Debug.Log($"Enemy Multiplier: {GetCurrentEnemySpawnMultiplier()}x");

        if (PersistentDataManager.Instance != null)
        {
            var session = PersistentDataManager.Instance.currentSession;
            Debug.Log($"Coins: {session.coins}");
            Debug.Log($"Health: {session.currentHealth}/{session.maxHealth}");
        }

        Debug.Log("====================================");
    }


    [ContextMenu("Force Complete Stage")]
    public void ForceCompleteStage()
    {
        int levelsRemaining = stageConfigs[currentStageIndex].numberOfLevels - currentLevelIndex;

        for (int i = 0; i < levelsRemaining; i++)
        {
            CompleteCurrentLevel();
        }
    }

    public void SkipToStage(int stageNumber)
    {
        if (stageNumber < 1 || stageNumber > stageConfigs.Length)
        {
            Debug.LogError($"Invalid stage number: {stageNumber}");
            return;
        }

        currentStageIndex = stageNumber - 1;
        currentLevelIndex = 0;

        totalLevelsCompleted = 0;
        for (int i = 0; i < currentStageIndex; i++)
        {
            totalLevelsCompleted += stageConfigs[i].numberOfLevels;
        }

        Debug.Log($"<color=cyan>Skipped to Stage {stageNumber}</color>");
        GenerateCurrentDungeon();
    }
}
