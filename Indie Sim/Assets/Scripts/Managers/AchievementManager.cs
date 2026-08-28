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

    // Achievement definitions. Unlock STATE is not stored here — it lives
    // solely in GameSession.Persistent.UnlockedAchievementIds (see IsUnlocked).
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

        InitializeAchievements();
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

    /// <summary>Sole source of truth for unlock state — GameSession.Persistent.</summary>
    private bool IsUnlocked(AchievementData achievement)
    {
        return GameSession.Instance != null
            && GameSession.Instance.Persistent.UnlockedAchievementIds.Contains(achievement.id);
    }

    /// <summary>
    /// Updates the UI text in the main menu to show current progress
    /// Call this when the Achievement Menu Panel is opened
    /// </summary>
    public void UpdateAchievementUI()
    {
        if (GameSession.Instance == null) return;

        // Update Kill Achievement UI
        if (killAchievementTitleCount != null)
        {
            int currentKills = GameSession.Instance.Persistent.TotalEnemiesKilled;
            AchievementData killAchievement = allAchievements.Find(a => a.type == AchievementType.TotalKills);

            if (killAchievement != null)
            {
                if (IsUnlocked(killAchievement))
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
            int currentCoins = GameSession.Instance.Persistent.TotalCoinsEverCollected;
            AchievementData coinAchievement = allAchievements.Find(a => a.type == AchievementType.TotalCoins);

            if (coinAchievement != null)
            {
                if (IsUnlocked(coinAchievement))
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
        if (GameSession.Instance == null) return;
        int totalKills = GameSession.Instance.Persistent.TotalEnemiesKilled;

        AchievementData killAchievement = allAchievements.Find(a => a.type == AchievementType.TotalKills);
        if (killAchievement != null && !IsUnlocked(killAchievement))
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
        if (GameSession.Instance == null) return;
        int totalCoins = GameSession.Instance.Persistent.TotalCoinsEverCollected;

        AchievementData coinAchievement = allAchievements.Find(a => a.type == AchievementType.TotalCoins);
        if (coinAchievement != null && !IsUnlocked(coinAchievement))
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
        if (relicAchievement != null && IsUnlocked(relicAchievement))
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
            if (relicAchievement != null && !IsUnlocked(relicAchievement))
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
        if (GameSession.Instance == null || IsUnlocked(achievement))
        {
            return; // Already unlocked (or no session to unlock into)
        }

        GameSession.Instance.Persistent.UnlockedAchievementIds.Add(achievement.id);
        GameSession.Instance.Save();

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
        if (IsUnlocked(achievement))
        {
            return 1f;
        }

        if (GameSession.Instance == null) return 0f;

        int currentValue = 0;

        switch (achievement.type)
        {
            case AchievementType.TotalKills:
                currentValue = GameSession.Instance.Persistent.TotalEnemiesKilled;
                break;

            case AchievementType.TotalCoins:
                currentValue = GameSession.Instance.Persistent.TotalCoinsEverCollected;
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
        if (GameSession.Instance == null) return 0;

        switch (achievement.type)
        {
            case AchievementType.TotalKills:
                return GameSession.Instance.Persistent.TotalEnemiesKilled;

            case AchievementType.TotalCoins:
                return GameSession.Instance.Persistent.TotalCoinsEverCollected;

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
        return achievement != null && IsUnlocked(achievement);
    }

    /// <summary>
    /// DEBUG: Reset all achievements
    /// </summary>
    public void DEBUG_ResetAllAchievements()
    {
        if (GameSession.Instance == null) return;

        GameSession.Instance.Persistent.UnlockedAchievementIds.Clear();
        GameSession.Instance.Save();

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
        if (GameSession.Instance == null) return;

        Debug.Log("========== ACHIEVEMENT STATUS ==========");
        Debug.Log($"Total Kills: {GameSession.Instance.Persistent.TotalEnemiesKilled}");
        Debug.Log($"Total Coins: {GameSession.Instance.Persistent.TotalCoinsEverCollected}");
        Debug.Log($"Unlocked Achievements:");

        foreach (AchievementData achievement in allAchievements)
        {
            string status = IsUnlocked(achievement) ? "✓ UNLOCKED" : "✗ LOCKED";
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
    public string rewardDescription; // Optional: What player gets for unlocking this

    public AchievementData(string id, string name, string description, AchievementType type, int requiredAmount, string rewardDescription = "")
    {
        this.id = id;
        this.name = name;
        this.description = description;
        this.type = type;
        this.requiredAmount = requiredAmount;
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
