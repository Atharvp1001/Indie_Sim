using UnityEngine;

public class PersistentDataManager : MonoBehaviour
{
    public static PersistentDataManager Instance { get; private set; }

    public GameSessionData currentSession;

    // Store the selected stage configuration
    public StageConfigSO selectedStageConfig;

    void Awake()
    {
        // Singleton pattern - only one instance can exist
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize new session
        currentSession = new GameSessionData();

        Debug.Log("PersistentDataManager initialized");
    }

    /// <summary>
    /// Start a new game run with fresh data
    /// </summary>
    public void StartNewRun()
    {
        currentSession = new GameSessionData();
        Debug.Log("New run started - all data reset");
    }

    /// <summary>
    /// Set which stage the player selected from the menu
    /// </summary>
    public void SetSelectedStage(StageConfigSO stageConfig)
    {
        selectedStageConfig = stageConfig;
        currentSession.currentStageIndex = 0; // Reset to first level of this stage
        currentSession.currentLevelIndex = 0;
        Debug.Log($"Selected Stage: {stageConfig.stageNumber} - {stageConfig.name}");
    }

    /// <summary>
    /// Save progression data
    /// </summary>
    public void SaveProgress(int stageIndex, int levelIndex, int totalCompleted)
    {
        currentSession.currentStageIndex = stageIndex;
        currentSession.currentLevelIndex = levelIndex;
        currentSession.totalLevelsCompleted = totalCompleted;
    }

    /// <summary>
    /// Add coins to the player
    /// </summary>
    public void AddCoins(int amount)
    {
        currentSession.coins += amount;
        Debug.Log($"Coins +{amount} = {currentSession.coins} total");
    }

    /// <summary>
    /// Try to spend coins - returns true if successful
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (currentSession.coins >= amount)
        {
            currentSession.coins -= amount;
            Debug.Log($"Spent {amount} coins. Remaining: {currentSession.coins}");
            return true;
        }

        Debug.LogWarning($"Not enough coins! Need {amount}, have {currentSession.coins}");
        return false;
    }

    /// <summary>
    /// Heal the player
    /// </summary>
    public void Heal(float amount)
    {
        currentSession.currentHealth = Mathf.Min(currentSession.currentHealth + amount, currentSession.maxHealth);
        Debug.Log($"Healed {amount}. Health: {currentSession.currentHealth}/{currentSession.maxHealth}");
    }

    /// <summary>
    /// Damage the player
    /// </summary>
    public void TakeDamage(float amount)
    {
        currentSession.currentHealth = Mathf.Max(0, currentSession.currentHealth - amount);
        Debug.Log($"Took {amount} damage. Health: {currentSession.currentHealth}/{currentSession.maxHealth}");
    }

    /// <summary>
    /// Check if player is dead
    /// </summary>
    public bool IsDead()
    {
        return currentSession.currentHealth <= 0;
    }

    /// <summary>
    /// Print current session info for debugging
    /// </summary>
    [ContextMenu("Print Session Info")]
    public void PrintSessionInfo()
    {
        Debug.Log("========== SESSION INFO ==========");
        Debug.Log($"Stage: {currentSession.currentStageIndex}, Level: {currentSession.currentLevelIndex}");
        Debug.Log($"Health: {currentSession.currentHealth}/{currentSession.maxHealth}");
        Debug.Log($"Coins: {currentSession.coins}");
        Debug.Log($"Damage Multiplier: {currentSession.damageMultiplier}x");
        Debug.Log($"Attack Speed Multiplier: {currentSession.attackSpeedMultiplier}x");
        Debug.Log($"Move Speed Multiplier: {currentSession.moveSpeedMultiplier}x");
        Debug.Log($"Applied Upgrades: {currentSession.appliedUpgradeIds.Count}");
        Debug.Log("==================================");
    }
}
