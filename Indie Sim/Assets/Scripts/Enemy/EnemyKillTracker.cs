using UnityEngine;

public class EnemyKillTracker : MonoBehaviour
{
    // Singleton instance - allows access from anywhere using EnemyKillTracker.Instance
    public static EnemyKillTracker Instance { get; private set; }

    

    [SerializeField] private int killsThisRun = 0;

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
    }

    /// <summary>
    /// Call this method whenever an enemy dies
    /// </summary>
    public void RegisterEnemyKill()
    {
        killsThisRun++;

        if (GameSession.Instance != null)
        {
            GameSession.Instance.Persistent.TotalEnemiesKilled++;
            GameSession.Instance.Save();
        }

        if (showDebugLogs)
            Debug.Log($"Enemy killed! Run kills: {killsThisRun} | Total: {GetTotalKills()}");

        if (AchievementManager.Instance != null)
            AchievementManager.Instance.CheckKillAchievements();
    }

    // ✅ NEW — getter for this run only
    public int GetKillsThisRun() => killsThisRun;

    // ✅ NEW — call this on retry/new run
    public void ResetRunKills()
    {
        killsThisRun = 0;
        Debug.Log("[EnemyKillTracker] Run kills reset");
    }

    /// <summary>
    /// Get the current total enemy kill count (lifetime, GameSession.Persistent).
    /// </summary>
    public int GetTotalKills()
    {
        return GameSession.Instance != null ? GameSession.Instance.Persistent.TotalEnemiesKilled : 0;
    }

    /// <summary>
    /// Reset kill count (useful for testing or "New Game")
    /// </summary>
    public void ResetKillCount()
    {
        if (GameSession.Instance == null) return;

        GameSession.Instance.Persistent.TotalEnemiesKilled = 0;
        GameSession.Instance.Save();

        if (showDebugLogs)
        {
            Debug.Log("Kill count reset to 0");
        }
    }

    // Auto-save when application quits
    private void OnApplicationQuit()
    {
        GameSession.Instance?.Save();
    }

    // Auto-save when application loses focus (for mobile/alt-tab)
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            GameSession.Instance?.Save();
        }
    }

    /// <summary>
    /// DEBUG: Print current kill status
    /// </summary>
    public void DEBUG_PrintKillStatus()
    {
        Debug.Log($"========== KILL STATUS ==========");
        Debug.Log($"Total Enemies Killed: {GetTotalKills()}");
        Debug.Log($"=================================");
    }
}
