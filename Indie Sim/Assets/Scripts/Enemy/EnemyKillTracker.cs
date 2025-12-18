using UnityEngine;

public class EnemyKillTracker : MonoBehaviour
{
    // Singleton instance - allows access from anywhere using EnemyKillTracker.Instance
    public static EnemyKillTracker Instance { get; private set; }

    [Header("Kill Statistics")]
    [SerializeField] private int totalEnemiesKilled = 0;

    // Key used to save/load data from PlayerPrefs
    private const string KILL_COUNT_KEY = "TotalEnemiesKilled";

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private void Awake()
    {
        // Singleton pattern - ensures only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Persists across scene changes

        // Load saved kill count when game starts
        LoadKillCount();
    }

    /// <summary>
    /// Call this method whenever an enemy dies
    /// </summary>
    public void RegisterEnemyKill()
    {
        totalEnemiesKilled++;

        if (showDebugLogs)
        {
            Debug.Log($"Enemy killed! Total kills: {totalEnemiesKilled}");
        }

        // Save immediately after each kill
        SaveKillCount();

        // ** ACHIEVEMENT INTEGRATION **
        // Notify achievement manager to check if any kill achievements were unlocked
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.CheckKillAchievements();
        }
        else
        {
            Debug.LogWarning("[EnemyKillTracker] AchievementManager not found. Achievement checks skipped.");
        }
    }

    /// <summary>
    /// Get the current total enemy kill count
    /// </summary>
    public int GetTotalKills()
    {
        return totalEnemiesKilled;
    }

    /// <summary>
    /// Manually save kill count to persistent storage
    /// </summary>
    public void SaveKillCount()
    {
        PlayerPrefs.SetInt(KILL_COUNT_KEY, totalEnemiesKilled);
        PlayerPrefs.Save(); // Force save immediately

        if (showDebugLogs)
        {
            Debug.Log($"Kill count saved: {totalEnemiesKilled}");
        }
    }

    /// <summary>
    /// Load kill count from persistent storage
    /// </summary>
    private void LoadKillCount()
    {
        totalEnemiesKilled = PlayerPrefs.GetInt(KILL_COUNT_KEY, 0);

        if (showDebugLogs)
        {
            Debug.Log($"Kill count loaded: {totalEnemiesKilled}");
        }
    }

    /// <summary>
    /// Reset kill count (useful for testing or "New Game")
    /// </summary>
    public void ResetKillCount()
    {
        totalEnemiesKilled = 0;
        SaveKillCount();

        if (showDebugLogs)
        {
            Debug.Log("Kill count reset to 0");
        }
    }

    // Auto-save when application quits
    private void OnApplicationQuit()
    {
        SaveKillCount();
    }

    // Auto-save when application loses focus (for mobile/alt-tab)
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveKillCount();
        }
    }

    /// <summary>
    /// DEBUG: Print current kill status
    /// </summary>
    public void DEBUG_PrintKillStatus()
    {
        Debug.Log($"========== KILL STATUS ==========");
        Debug.Log($"Total Enemies Killed: {totalEnemiesKilled}");
        Debug.Log($"=================================");
    }
}
