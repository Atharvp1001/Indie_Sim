using UnityEngine;
/*
public class PersistentDataManager : MonoBehaviour
{
    public static PersistentDataManager Instance { get; private set; }

    public GameSessionData currentSession;

    // Store the selected stage configuration
    public StageConfigSO selectedStageConfig;

    [Header("Debug")]
    public bool debugMode = true;

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

        // Load existing session or create new one
        LoadSessionData();

        Debug.Log("<color=cyan>PersistentDataManager initialized</color>");
    }

    void OnApplicationQuit()
    {
        // Save everything when game closes
        SaveSessionData();
    }

    // ========== SAVE/LOAD METHODS ==========

    /// <summary>
    /// Load session data from PlayerPrefs (using JSON serialization)
    /// </summary>
    void LoadSessionData()
    {
        string json = PlayerPrefs.GetString("GameSessionData", "");

        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                currentSession = JsonUtility.FromJson<GameSessionData>(json);

                if (debugMode)
                {
                    Debug.Log("<color=green>✓ LOADED session data from PlayerPrefs</color>");
                    PrintSessionInfo();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load session data: {e}");
                currentSession = new GameSessionData();
            }
        }
        else
        {
            // First time - create new session
            currentSession = new GameSessionData();

            if (debugMode)
            {
                Debug.Log("<color=yellow>First time - created NEW session data</color>");
            }
        }
    }

    /// <summary>
    /// Save session data to PlayerPrefs (using JSON serialization)
    /// </summary>
    public void SaveSessionData()
    {
        if (currentSession == null)
        {
            Debug.LogError("Cannot save - currentSession is null!");
            return;
        }

        string json = JsonUtility.ToJson(currentSession, prettyPrint: true);
        PlayerPrefs.SetString("GameSessionData", json);
        PlayerPrefs.Save();

        if (debugMode)
        {
            Debug.Log("<color=green>✓ SAVED session data to PlayerPrefs</color>");
            Debug.Log($"<color=cyan>JSON:\n{json}</color>");
        }
    }

    // ========== GAME SESSION MANAGEMENT ==========

    /// <summary>
    /// Start a new game run with fresh data
    /// </summary>
    public void StartNewRun()
    {
        currentSession = new GameSessionData();
        SaveSessionData();

        Debug.Log("<color=yellow>New run started - all data reset</color>");
    }

    /// <summary>
    /// Set which stage the player selected from the menu
    /// </summary>
    public void SetSelectedStage(StageConfigSO stageConfig)
    {
        selectedStageConfig = stageConfig;
        currentSession.currentStageIndex = stageConfig.stageNumber - 1; // Convert to 0-based
        currentSession.currentLevelIndex = 0;

        SaveSessionData();

        Debug.Log($"<color=cyan>Selected Stage: {stageConfig.stageNumber} - {stageConfig.name}</color>");
    }

    /// <summary>
    /// Save progression data
    /// </summary>
    public void SaveProgress(int stageIndex, int levelIndex, int totalCompleted)
    {
        currentSession.currentStageIndex = stageIndex;
        currentSession.currentLevelIndex = levelIndex;
        currentSession.totalLevelsCompleted = totalCompleted;

        SaveSessionData();
    }

    // ========== COIN MANAGEMENT ==========

    /// <summary>
    /// Add coins to the player
    /// </summary>
    public void AddCoins(int amount)
    {
        currentSession.coins += amount;
        SaveSessionData();

        Debug.Log($"<color=yellow>Coins +{amount} = {currentSession.coins} total</color>");
    }

    /// <summary>
    /// Try to spend coins - returns true if successful
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (currentSession.coins >= amount)
        {
            currentSession.coins -= amount;
            SaveSessionData();

            Debug.Log($"<color=lime>Spent {amount} coins. Remaining: {currentSession.coins}</color>");
            return true;
        }

        Debug.LogWarning($"<color=red>Not enough coins! Need {amount}, have {currentSession.coins}</color>");
        return false;
    }

    /// <summary>
    /// Set coins to a specific amount
    /// </summary>
    public void SetCoins(int amount)
    {
        currentSession.coins = Mathf.Max(0, amount);
        SaveSessionData();

        Debug.Log($"<color=cyan>Coins set to {currentSession.coins}</color>");
    }

    // ========== HEALTH MANAGEMENT ==========

    /// <summary>
    /// Heal the player
    /// </summary>
    public void Heal(float amount)
    {
        currentSession.currentHealth = Mathf.Min(currentSession.currentHealth + amount, currentSession.maxHealth);
        SaveSessionData();

        Debug.Log($"<color=green>Healed {amount}. Health: {currentSession.currentHealth}/{currentSession.maxHealth}</color>");
    }

    /// <summary>
    /// Damage the player
    /// </summary>
    public void TakeDamage(float amount)
    {
        currentSession.currentHealth = Mathf.Max(0, currentSession.currentHealth - amount);
        SaveSessionData();

        Debug.Log($"<color=red>Took {amount} damage. Health: {currentSession.currentHealth}/{currentSession.maxHealth}</color>");
    }

    /// <summary>
    /// Check if player is dead
    /// </summary>
    public bool IsDead()
    {
        return currentSession.currentHealth <= 0;
    }

    /// <summary>
    /// Set max health
    /// </summary>
    public void SetMaxHealth(float maxHealth)
    {
        currentSession.maxHealth = Mathf.Max(1, maxHealth);
        currentSession.currentHealth = Mathf.Min(currentSession.currentHealth, currentSession.maxHealth);
        SaveSessionData();

        Debug.Log($"<color=cyan>Max health set to {currentSession.maxHealth}</color>");
    }

    // ========== UPGRADE MANAGEMENT ==========

    /// <summary>
    /// Update player health upgrade level
    /// </summary>
    public void UpdateHealthUpgradeLevel(int level)
    {
        currentSession.healthUpgradeLevel = level;
        currentSession.maxHealthBonus = level * 25; // 25 per level
        SaveSessionData();

        Debug.Log($"<color=yellow>Health upgrade level: {level}, Bonus: +{currentSession.maxHealthBonus}</color>");
    }

    /// <summary>
    /// Update player speed upgrade level
    /// </summary>
    public void UpdateSpeedUpgradeLevel(int level)
    {
        currentSession.speedUpgradeLevel = level;
        SaveSessionData();

        Debug.Log($"<color=yellow>Speed upgrade level: {level}</color>");
    }

    /// <summary>
    /// Update weapon damage upgrade
    /// </summary>
    public void UpdateWeaponDamageUpgrade(string weaponName, int bonus)
    {
        if (weaponName == "Pistol")
            currentSession.pistolDamageBonus = bonus;
        else if (weaponName == "MachineGun")
            currentSession.MachineGunDamageBonus = bonus;
        else if (weaponName == "Shotgun")
            currentSession.shotgunDamageBonus = bonus;

        SaveSessionData();

        Debug.Log($"<color=yellow>{weaponName} damage upgrade: +{bonus}</color>");
    }

    /// <summary>
    /// Get current upgrade status
    /// </summary>
    public void PrintUpgradeStatus()
    {
        Debug.Log("<color=yellow>========== UPGRADE STATUS ==========</color>");
        Debug.Log($"Health Level: {currentSession.healthUpgradeLevel}, Bonus: +{currentSession.maxHealthBonus}");
        Debug.Log($"Speed Level: {currentSession.speedUpgradeLevel}");
        Debug.Log($"Pistol Damage: +{currentSession.pistolDamageBonus}");
        Debug.Log($"MachineGun Damage: +{currentSession.MachineGunDamageBonus}");
        Debug.Log($"Shotgun Damage: +{currentSession.shotgunDamageBonus}");
        Debug.Log("<color=yellow>====================================</color>");
    }

    // ========== DEBUG/UTILITY METHODS ==========

    /// <summary>
    /// Print current session info for debugging
    /// </summary>
    [ContextMenu("Print Session Info")]
    public void PrintSessionInfo()
    {
        Debug.Log("<color=cyan>========== SESSION INFO ==========</color>");
        Debug.Log($"<color=yellow>Stage: {currentSession.GetCurrentStageNumber()}, Level: {currentSession.GetCurrentLevelNumber()}</color>");
        Debug.Log($"<color=yellow>Total Levels Completed: {currentSession.totalLevelsCompleted}</color>");
        Debug.Log($"<color=lime>Health: {currentSession.currentHealth}/{currentSession.maxHealth}</color>");
        Debug.Log($"<color=lime>Coins: {currentSession.coins}</color>");
        Debug.Log($"<color=cyan>Damage Multiplier: {currentSession.damageMultiplier}x</color>");
        Debug.Log($"<color=cyan>Attack Speed Multiplier: {currentSession.attackSpeedMultiplier}x</color>");
        Debug.Log($"<color=cyan>Move Speed Multiplier: {currentSession.moveSpeedMultiplier}x</color>");
        Debug.Log($"<color=cyan>Max Health Bonus: +{currentSession.maxHealthBonus}</color>");
        Debug.Log($"<color=cyan>Applied Upgrades: {currentSession.appliedUpgradeIds.Count}</color>");
        Debug.Log("<color=cyan>==================================</color>");
    }

    /// <summary>
    /// Reset all data (for testing)
    /// </summary>
    [ContextMenu("DEBUG: Clear All Data")]
    public void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        currentSession = new GameSessionData();

        Debug.Log("<color=red>✓ Cleared all PlayerPrefs data</color>");
    }

    /// <summary>
    /// Print raw JSON data
    /// </summary>
    [ContextMenu("DEBUG: Print Raw JSON")]
    public void PrintRawJSON()
    {
        string json = PlayerPrefs.GetString("GameSessionData", "No data saved");
        Debug.Log($"<color=magenta>RAW JSON:\n{json}</color>");
    }
}
*/