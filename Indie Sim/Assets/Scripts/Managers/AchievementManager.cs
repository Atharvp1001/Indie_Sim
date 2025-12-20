using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class AchievementManager : MonoBehaviour
{
    // Singleton instance
    public static AchievementManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("UI - Set Dynamically By Scene")]
    private TextMeshProUGUI killAchievementTitleCount;
    private TextMeshProUGUI CoinAchievementTitleCount;


    // PlayerPrefs keys for persistent data
    private const string TOTAL_COINS_KEY = "TotalCoinsEverCollected";
    private const string TOTAL_KILLS_KEY = "TotalEnemiesKilled";
    private const string ACHIEVEMENTS_KEY = "UnlockedAchievements"; // Stores comma-separated achievement IDs

    // Achievement definitions
    private List<AchievementData> allAchievements = new List<AchievementData>();

    // Events for UI
    public System.Action<AchievementData> OnAchievementUnlocked;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAchievements();
        LoadUnlockedAchievements();
    }

    /// <summary>
    /// Define all achievements in the game (ONLY 3 ACHIEVEMENTS)
    /// </summary>
    private void InitializeAchievements()
    {
        allAchievements.Clear();

        // KILL ACHIEVEMENT - Kill 1000 enemies total
        allAchievements.Add(new AchievementData(
            id: "kill_1000",
            name: "Legendary Executioner",
            description: "Kill 1000 enemies",
            type: AchievementType.TotalKills,
            requiredAmount: 1000
        ));

        // COIN ACHIEVEMENT - Collect 1000 coins total
        allAchievements.Add(new AchievementData(
            id: "coin_1000",
            name: "Golden Hoarder",
            description: "Collect 1000 coins",
            type: AchievementType.TotalCoins,
            requiredAmount: 1000
        ));

        // RELIC ACHIEVEMENT - Collect all 8 relics in a single run
        // Reward: Shotgun (to be implemented)
        allAchievements.Add(new AchievementData(
            id: "relic_all_complete",
            name: "Relic Hunter",
            description: "Collect all 8 relics in a single run",
            type: AchievementType.RelicRun,
            requiredAmount: 8,
            rewardDescription: "Unlocks: Shotgun"
        ));

        if (showDebugLogs)
        {
            Debug.Log($"[AchievementManager] Initialized {allAchievements.Count} achievements");
        }
    }

    /// <summary>
    /// Load which achievements have been unlocked from PlayerPrefs
    /// </summary>
    private void LoadUnlockedAchievements()
    {
        string unlockedIDs = PlayerPrefs.GetString(ACHIEVEMENTS_KEY, "");

        if (string.IsNullOrEmpty(unlockedIDs))
        {
            if (showDebugLogs)
            {
                Debug.Log("[AchievementManager] No achievements unlocked yet");
            }
            return;
        }

        string[] ids = unlockedIDs.Split(',');
        foreach (string id in ids)
        {
            AchievementData achievement = allAchievements.Find(a => a.id == id);
            if (achievement != null)
            {
                achievement.isUnlocked = true;
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[AchievementManager] Loaded {ids.Length} unlocked achievements");
        }
    }

    /// <summary>
    /// Updates the UI text in the main menu to show current progress
    /// Call this when the Achievement Menu Panel is opened
    /// </summary>
    public void UpdateAchievementUI()
    {
        // Update Kill Achievement UI
        if (killAchievementTitleCount != null)
        {
            int currentKills = PlayerPrefs.GetInt(TOTAL_KILLS_KEY, 0);
            AchievementData killAchievement = allAchievements.Find(a => a.type == AchievementType.TotalKills);

            if (killAchievement != null)
            {
                if (killAchievement.isUnlocked)
                {
                    killAchievementTitleCount.text = $"{killAchievement.requiredAmount}/{killAchievement.requiredAmount}";
                }
                else
                {
                    killAchievementTitleCount.text = $"{currentKills}/{killAchievement.requiredAmount}";
                }
            }

            if (showDebugLogs)
            {
                Debug.Log($"[AchievementManager] Updated Kill UI: {currentKills}");
            }
        }

        // Update Coin Achievement UI
        if (CoinAchievementTitleCount != null)
        {
            int currentCoins = PlayerPrefs.GetInt(TOTAL_COINS_KEY, 0);
            AchievementData coinAchievement = allAchievements.Find(a => a.type == AchievementType.TotalCoins);

            if (coinAchievement != null)
            {
                if (coinAchievement.isUnlocked)
                {
                    CoinAchievementTitleCount.text = $"{coinAchievement.requiredAmount}/{coinAchievement.requiredAmount}";
                }
                else
                {
                    CoinAchievementTitleCount.text = $"{currentCoins}/{coinAchievement.requiredAmount}";
                }
            }

            if (showDebugLogs)
            {
                Debug.Log($"[AchievementManager] Updated Coin UI: {currentCoins}");
            }
        }

       
    }

    /// <summary>
    /// Set UI references from the current scene
    /// Call this when loading the main menu scene
    /// </summary>
    public void SetUIReferences(TextMeshProUGUI killUI, TextMeshProUGUI coinUI)
    {
        killAchievementTitleCount = killUI;
        CoinAchievementTitleCount = coinUI;

        if (showDebugLogs)
        {
            Debug.Log("[AchievementManager] UI references updated for current scene");
        }
    }


    /// <summary>
    /// Called when player kills an enemy - checks kill achievement
    /// Call this from EnemyKillTracker after incrementing kill count
    /// </summary>
    public void CheckKillAchievements()
    {
        int totalKills = PlayerPrefs.GetInt(TOTAL_KILLS_KEY, 0);

        AchievementData killAchievement = allAchievements.Find(a => a.type == AchievementType.TotalKills);
        if (killAchievement != null && !killAchievement.isUnlocked)
        {
            if (totalKills >= killAchievement.requiredAmount)
            {
                UnlockAchievement(killAchievement);
            }
        }
    }

    /// <summary>
    /// Called when player collects a coin - checks coin achievement
    /// Call this from CoinManager after adding coins
    /// </summary>
    public void CheckCoinAchievements()
    {
        int totalCoins = PlayerPrefs.GetInt(TOTAL_COINS_KEY, 0);

        AchievementData coinAchievement = allAchievements.Find(a => a.type == AchievementType.TotalCoins);
        if (coinAchievement != null && !coinAchievement.isUnlocked)
        {
            if (totalCoins >= coinAchievement.requiredAmount)
            {
                UnlockAchievement(coinAchievement);
            }
        }
    }

    /// <summary>
    /// Called when player collects a relic in current run
    /// </summary>
    public void OnRelicCollected(int totalRelicsInRun)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[AchievementManager] Relic collected. Total in run: {totalRelicsInRun}/8");
        }

        // Check if achievement is already unlocked
        AchievementData relicAchievement = allAchievements.Find(a => a.id == "relic_all_complete");
        if (relicAchievement != null && relicAchievement.isUnlocked)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[AchievementManager] Relic achievement already unlocked. Skipping.");
            }
            return; // Don't unlock again
        }

        // Check if all 8 relics collected
        if (totalRelicsInRun >= 8)
        {
            if (relicAchievement != null && !relicAchievement.isUnlocked)
            {
                UnlockAchievement(relicAchievement);
            }
        }
    }

    /// <summary>
    /// Unlocks an achievement and saves it
    /// </summary>
    private void UnlockAchievement(AchievementData achievement)
    {
        if (achievement.isUnlocked)
        {
            return; // Already unlocked
        }

        achievement.isUnlocked = true;

        // Save to PlayerPrefs
        string currentUnlocked = PlayerPrefs.GetString(ACHIEVEMENTS_KEY, "");
        if (string.IsNullOrEmpty(currentUnlocked))
        {
            currentUnlocked = achievement.id;
        }
        else
        {
            currentUnlocked += "," + achievement.id;
        }

        PlayerPrefs.SetString(ACHIEVEMENTS_KEY, currentUnlocked);
        PlayerPrefs.Save();

        // Notify listeners (for UI popup)
        OnAchievementUnlocked?.Invoke(achievement);

        if (showDebugLogs)
        {
            Debug.Log($"🏆 [ACHIEVEMENT UNLOCKED] {achievement.name}: {achievement.description}");
        }

        // ** FUTURE: GRANT REWARDS HERE **
        GrantAchievementReward(achievement);
    }

    /// <summary>
    /// Grant rewards for unlocking achievements
    /// TODO: Implement reward system (weapons, items, etc.)
    /// </summary>
    private void GrantAchievementReward(AchievementData achievement)
    {
        // Check which achievement was unlocked and grant appropriate reward
        switch (achievement.id)
        {
            case "relic_all_complete":
                // TODO: Unlock shotgun weapon
                Debug.Log("[AchievementManager] 🎁 REWARD: Shotgun unlocked! (To be implemented)");
                // Example: WeaponManager.Instance.UnlockWeapon("Shotgun");
                break;

            // You can add more reward cases here in the future
            // case "kill_1000":
            //     // TODO: Grant reward for killing 1000 enemies
            //     break;

            // case "coin_1000":
            //     // TODO: Grant reward for collecting 1000 coins
            //     break;

            default:
                break;
        }
    }

    /// <summary>
    /// Get all achievements (for UI display)
    /// </summary>
    public List<AchievementData> GetAllAchievements()
    {
        return new List<AchievementData>(allAchievements);
    }

    /// <summary>
    /// Get current progress for an achievement (0 to 1)
    /// </summary>
    public float GetAchievementProgress(AchievementData achievement)
    {
        if (achievement.isUnlocked)
        {
            return 1f;
        }

        int currentValue = 0;

        switch (achievement.type)
        {
            case AchievementType.TotalKills:
                currentValue = PlayerPrefs.GetInt(TOTAL_KILLS_KEY, 0);
                break;

            case AchievementType.TotalCoins:
                currentValue = PlayerPrefs.GetInt(TOTAL_COINS_KEY, 0);
                break;

            case AchievementType.RelicRun:
                // This is tracked by RelicManager per run
                return 0f; // Can't show progress for run-based achievements
        }

        return Mathf.Clamp01((float)currentValue / achievement.requiredAmount);
    }

    /// <summary>
    /// Get current progress value (e.g., 150/1000 kills)
    /// </summary>
    public int GetCurrentProgressValue(AchievementData achievement)
    {
        switch (achievement.type)
        {
            case AchievementType.TotalKills:
                return PlayerPrefs.GetInt(TOTAL_KILLS_KEY, 0);

            case AchievementType.TotalCoins:
                return PlayerPrefs.GetInt(TOTAL_COINS_KEY, 0);

            default:
                return 0;
        }
    }


   

    /// <summary>
    /// Check if a specific achievement is unlocked
    /// Useful for checking if player has earned a reward
    /// </summary>
    public bool IsAchievementUnlocked(string achievementId)
    {
        AchievementData achievement = allAchievements.Find(a => a.id == achievementId);
        return achievement != null && achievement.isUnlocked;
    }

    /// <summary>
    /// DEBUG: Reset all achievements
    /// </summary>
    public void DEBUG_ResetAllAchievements()
    {
        PlayerPrefs.DeleteKey(ACHIEVEMENTS_KEY);
        PlayerPrefs.Save();

        foreach (AchievementData achievement in allAchievements)
        {
            achievement.isUnlocked = false;
        }

        if (showDebugLogs)
        {
            Debug.Log("[AchievementManager] All achievements reset!");
        }
    }

    /// <summary>
    /// DEBUG: Print achievement status
    /// </summary>
    public void DEBUG_PrintAchievementStatus()
    {
        Debug.Log("========== ACHIEVEMENT STATUS ==========");
        Debug.Log($"Total Kills: {PlayerPrefs.GetInt(TOTAL_KILLS_KEY, 0)}");
        Debug.Log($"Total Coins: {PlayerPrefs.GetInt(TOTAL_COINS_KEY, 0)}");
        Debug.Log($"Unlocked Achievements:");

        foreach (AchievementData achievement in allAchievements)
        {
            string status = achievement.isUnlocked ? "✓ UNLOCKED" : "✗ LOCKED";
            string progress = achievement.type == AchievementType.RelicRun ? "Per Run" : $"{GetCurrentProgressValue(achievement)}/{achievement.requiredAmount}";
            Debug.Log($"  {status} - {achievement.name} ({progress})");
        }

        Debug.Log("========================================");
    }
}

/// <summary>
/// Achievement data structure
/// </summary>
[System.Serializable]
public class AchievementData
{
    public string id;
    public string name;
    public string description;
    public AchievementType type;
    public int requiredAmount;
    public bool isUnlocked;
    public string rewardDescription; // Optional: What player gets for unlocking this

    public AchievementData(string id, string name, string description, AchievementType type, int requiredAmount, string rewardDescription = "")
    {
        this.id = id;
        this.name = name;
        this.description = description;
        this.type = type;
        this.requiredAmount = requiredAmount;
        this.isUnlocked = false;
        this.rewardDescription = rewardDescription;
    }
}

/// <summary>
/// Types of achievements
/// </summary>
public enum AchievementType
{
    TotalKills,    // Based on total enemies killed ever
    TotalCoins,    // Based on total coins collected ever
    RelicRun       // Based on completing a run with all relics
}
