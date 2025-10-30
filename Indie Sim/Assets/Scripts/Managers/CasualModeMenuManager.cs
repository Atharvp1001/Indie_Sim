using UnityEngine;
using UnityEngine.SceneManagement;

public class CasualModeMenuManager : MonoBehaviour
{
    [Header("Stage Configuration")]
    [Tooltip("All stages in order - used to determine unlocking")]
    public StageConfigSO[] allStages;

    [Header("Stage Buttons")]
    [Tooltip("Stage buttons in the same order as allStages")]
    public StageButton[] stageButtons;

    [Header("Unlocking Settings")]
    [Tooltip("DEV MODE: Check this to unlock all stages for testing")]
    public bool unlockAllStagesForDev = true;

    [Tooltip("Which stages are unlocked by default in production (usually 1)")]
    public int defaultUnlockedStages = 1;

    void Start()
    {
        // Make sure PersistentDataManager exists
        EnsurePersistentManagerExists();

        // Validate setup
        if (allStages == null || allStages.Length == 0)
        {
            Debug.LogError("CasualModeMenuManager: No stages assigned!");
            return;
        }

        if (stageButtons == null || stageButtons.Length == 0)
        {
            Debug.LogError("CasualModeMenuManager: No stage buttons assigned!");
            return;
        }

        if (allStages.Length != stageButtons.Length)
        {
            Debug.LogWarning($"Number of stages ({allStages.Length}) doesn't match number of buttons ({stageButtons.Length})!");
        }

        // Load unlock progress
        LoadStageUnlockProgress();

        Debug.Log($"<color=cyan>Casual Mode Menu loaded with {stageButtons.Length} stages</color>");

        if (unlockAllStagesForDev)
        {
            Debug.Log("<color=yellow>DEV MODE: All stages unlocked for testing</color>");
        }

        PrintMenuStatus();
    }

    /// <summary>
    /// Ensure PersistentDataManager exists - create if needed
    /// </summary>
    void EnsurePersistentManagerExists()
    {
        if (PersistentDataManager.Instance == null)
        {
            GameObject managerObj = new GameObject("PersistentDataManager");
            managerObj.AddComponent<PersistentDataManager>();
            Debug.Log("Created PersistentDataManager automatically");
        }
    }

    /// <summary>
    /// Load which stages should be unlocked based on player progress
    /// </summary>
    void LoadStageUnlockProgress()
    {
        // DEV MODE: Unlock everything
        if (unlockAllStagesForDev)
        {
            for (int i = 0; i < stageButtons.Length; i++)
            {
                if (stageButtons[i] != null)
                {
                    stageButtons[i].isUnlocked = true;
                }
            }
            return;
        }

        // PRODUCTION MODE: Use saved progress
        int highestCompletedStage = PlayerPrefs.GetInt("HighestCompletedStage", 0);

        Debug.Log($"Highest completed stage: {highestCompletedStage}");

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] != null)
            {
                bool shouldUnlock = (i < defaultUnlockedStages) || (i <= highestCompletedStage);
                stageButtons[i].isUnlocked = shouldUnlock;

                Debug.Log($"Stage {i + 1}: {(shouldUnlock ? "UNLOCKED" : "LOCKED")}");
            }
        }
    }

    /// <summary>
    /// Call this when a stage is completed to unlock the next one
    /// </summary>
    public void CompleteStage(int stageNumber)
    {
        int stageIndex = stageNumber - 1;

        // Save progress
        PlayerPrefs.SetInt("HighestCompletedStage", stageIndex);
        PlayerPrefs.Save();

        Debug.Log($"<color=lime>Stage {stageNumber} marked as completed!</color>");

        // Unlock next stage button if it exists
        if (stageIndex + 1 < stageButtons.Length && stageButtons[stageIndex + 1] != null)
        {
            stageButtons[stageIndex + 1].UnlockStage();
            Debug.Log($"<color=yellow>Stage {stageIndex + 2} unlocked!</color>");
        }
        else
        {
            Debug.Log("<color=magenta>All stages completed! No more stages to unlock.</color>");
        }
    }

    /// <summary>
    /// Print current menu status for debugging
    /// </summary>
    [ContextMenu("Print Menu Status")]
    public void PrintMenuStatus()
    {
        Debug.Log("========== CASUAL MODE MENU STATUS ==========");
        Debug.Log($"DEV MODE: {(unlockAllStagesForDev ? "ON (All unlocked)" : "OFF (Production)")}");
        Debug.Log($"Total Stages: {allStages.Length}");
        Debug.Log($"Total Buttons: {stageButtons.Length}");
        Debug.Log($"Default Unlocked: {defaultUnlockedStages}");
        Debug.Log($"Highest Completed: {PlayerPrefs.GetInt("HighestCompletedStage", 0)}");

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] != null && allStages[i] != null)
            {
                string status = stageButtons[i].isUnlocked ? "UNLOCKED" : "LOCKED";
                Debug.Log($"  Stage {i + 1} ({allStages[i].name}): {status}");
            }
        }

        Debug.Log("=============================================");
    }

    // ============ DEBUG METHODS ============

    /// <summary>
    /// Debug: Reset all progress - lock everything except stage 1
    /// </summary>
    [ContextMenu("DEBUG: Reset Stage Progress")]
    public void ResetStageProgress()
    {
        PlayerPrefs.DeleteKey("HighestCompletedStage");
        PlayerPrefs.Save();

        Debug.Log("<color=red>DEBUG: Stage progress reset! Only Stage 1 is unlocked.</color>");

        // Reload scene to refresh visuals
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
