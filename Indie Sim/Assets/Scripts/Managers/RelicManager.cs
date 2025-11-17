using UnityEngine;

public class RelicManager : MonoBehaviour
{
    [Header("Relic Tracking")]
    [Tooltip("8 relics total (0-7). Tracks which ones have been collected this run.")]
    private bool[] relicsCollected = new bool[8];

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private void Start()
    {
        // Reset relics at start of run
        ResetForNewRun();
    }

    /// <summary>
    /// Called when player collects a relic
    /// </summary>
    public void CollectRelic(int relicIndex)
    {
        // Validate relic index
        if (relicIndex < 0 || relicIndex >= relicsCollected.Length)
        {
            Debug.LogError($"[RelicManager] Invalid relic index: {relicIndex}. Must be 0-7.");
            return;
        }

        // Check if already collected
        if (relicsCollected[relicIndex])
        {
            Debug.LogWarning($"[RelicManager] Relic {relicIndex} already collected!");
            return;
        }

        // Mark as collected
        relicsCollected[relicIndex] = true;

        int totalCollected = GetTotalRelicsCollected();

        if (showDebugInfo)
        {
            Debug.Log($"[RelicManager] Relic {relicIndex} collected! Total: {totalCollected}/8");
        }

        // Notify achievement manager
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.OnRelicCollected(totalCollected);
        }

        // Check if all relics collected
        if (totalCollected >= 8)
        {
            OnAllRelicsCollected();
        }
    }

    /// <summary>
    /// Get total number of relics collected this run
    /// </summary>
    public int GetTotalRelicsCollected()
    {
        int count = 0;
        foreach (bool collected in relicsCollected)
        {
            if (collected) count++;
        }
        return count;
    }

    /// <summary>
    /// Check if a specific relic has been collected
    /// </summary>
    public bool IsRelicCollected(int relicIndex)
    {
        if (relicIndex < 0 || relicIndex >= relicsCollected.Length)
        {
            return false;
        }
        return relicsCollected[relicIndex];
    }

    /// <summary>
    /// Called when all 8 relics are collected
    /// </summary>
    private void OnAllRelicsCollected()
    {
        Debug.Log($"[RelicManager] 🎉 ALL RELICS COLLECTED!");

        // You can trigger special events here
        // Example: Play special animation, show UI, etc.
    }

    /// <summary>
    /// Reset relics for a new run (call this when starting a new dungeon run)
    /// </summary>
    public void ResetForNewRun()
    {
        for (int i = 0; i < relicsCollected.Length; i++)
        {
            relicsCollected[i] = false;
        }

        if (showDebugInfo)
        {
            Debug.Log("[RelicManager] Relics reset for new run");
        }
    }

    /// <summary>
    /// DEBUG: Print current relic status
    /// </summary>
    public void DEBUG_PrintRelicStatus()
    {
        Debug.Log("========== RELIC STATUS ==========");
        Debug.Log($"Relics Collected: {GetTotalRelicsCollected()}/8");

        for (int i = 0; i < relicsCollected.Length; i++)
        {
            string status = relicsCollected[i] ? "✓" : "✗";
            Debug.Log($"  Relic {i}: {status}");
        }

        Debug.Log("==================================");
    }
}
