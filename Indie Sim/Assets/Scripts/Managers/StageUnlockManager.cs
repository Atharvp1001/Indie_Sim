using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StageUnlockManager : MonoBehaviour
{
    public static StageUnlockManager Instance { get; private set; }

    [Header("Stage Setup")]
    public StageConfigSO[] allStages;  // Array of stages in correct order

    [Header("Debug Settings")]
    [Tooltip("Unlock all stages for easy testing")]
    public bool unlockAllStagesForDebug = false;

    // Track which specific stages are completed (by index)
    private HashSet<int> completedStages = new HashSet<int>();

    private void Awake()
    {
        // Singleton pattern to persist across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadProgress();
    }

    /// <summary>
    /// Load stage unlock progress from PlayerPrefs
    /// </summary>
    private void LoadProgress()
    {
        // Load completed stages as a comma-separated string
        string completedStagesString = PlayerPrefs.GetString("CompletedStages", "");

        completedStages.Clear();

        if (!string.IsNullOrEmpty(completedStagesString))
        {
            string[] stageIndices = completedStagesString.Split(',');
            foreach (string indexStr in stageIndices)
            {
                if (int.TryParse(indexStr, out int index))
                {
                    completedStages.Add(index);
                }
            }
        }

        if (unlockAllStagesForDebug)
        {
            Debug.Log("<color=green>DEBUG MODE: All stages unlocked</color>");
        }
        else
        {
            Debug.Log($"<color=blue>Loaded completed stages: {completedStagesString}</color>");
        }
    }

    /// <summary>
    /// Save completed stages to PlayerPrefs
    /// </summary>
    private void SaveProgress()
    {
        // Convert HashSet to comma-separated string
        string completedStagesString = string.Join(",", completedStages.Select(x => x.ToString()));

        PlayerPrefs.SetString("CompletedStages", completedStagesString);
        PlayerPrefs.Save();

        Debug.Log($"<color=green>Saved completed stages: {completedStagesString}</color>");
    }

    /// <summary>
    /// Returns true if the given stage index is unlocked
    /// </summary>
    public bool IsStageUnlocked(int stageIndex)
    {
        // Debug mode unlocks everything
        if (unlockAllStagesForDebug)
            return true;

        // Stage 0 (first stage) is always unlocked
        if (stageIndex == 0)
            return true;

        // A stage is unlocked if the previous stage is completed
        int previousStageIndex = stageIndex - 1;
        return completedStages.Contains(previousStageIndex);
    }

    /// <summary>
    /// Called to mark a stage as completed and unlock next
    /// </summary>
    public void CompleteStage(int completedStageIndex)
    {
        if (completedStages.Contains(completedStageIndex))
        {
            Debug.Log($"<color=yellow>Stage {completedStageIndex + 1} already completed previously.</color>");
        }
        else
        {
            completedStages.Add(completedStageIndex);
            SaveProgress();
            Debug.Log($"<color=green>Stage {completedStageIndex + 1} completed for the first time!</color>");
        }

        // Check if next stage should be unlocked
        int nextStageIndex = completedStageIndex + 1;
        if (nextStageIndex < allStages.Length)
        {
            Debug.Log($"<color=lime>Stage {nextStageIndex + 1} is now unlocked!</color>");
        }
        else
        {
            Debug.Log("<color=magenta>★ ALL STAGES COMPLETED! ★</color>");
        }
    }

    /// <summary>
    /// Check if a specific stage has been completed
    /// </summary>
    public bool IsStageCompleted(int stageIndex)
    {
        return completedStages.Contains(stageIndex);
    }

    /// <summary>
    /// Reset all stage unlock progress
    /// </summary>
    [ContextMenu("DEBUG: Reset Stage Unlocks")]
    public void ResetStageUnlocks()
    {
        completedStages.Clear();
        PlayerPrefs.DeleteKey("CompletedStages");
        PlayerPrefs.Save();
        Debug.Log("<color=red>DEBUG: Reset all stage unlocks!</color>");

        // Refresh menu if it exists
        RefreshMenu();
    }

    /// <summary>
    /// Unlock all stages (debug)
    /// </summary>
    [ContextMenu("DEBUG: Unlock All Stages")]
    public void UnlockAllStages()
    {
        completedStages.Clear();
        for (int i = 0; i < allStages.Length - 1; i++) // Complete all except the last
        {
            completedStages.Add(i);
        }
        SaveProgress();
        Debug.Log("<color=green>DEBUG: Unlocked all stages!</color>");

        // Refresh menu if it exists
        RefreshMenu();
    }

    /// <summary>
    /// Reload progress from PlayerPrefs (useful if you change debug settings)
    /// </summary>
    [ContextMenu("DEBUG: Reload Progress")]
    public void ReloadProgress()
    {
        LoadProgress();
        RefreshMenu();
        Debug.Log("<color=cyan>Progress reloaded from PlayerPrefs</color>");
    }

    /// <summary>
    /// Helper to refresh the menu after unlock changes
    /// </summary>
    private void RefreshMenu()
    {
        CasualModeMenuManager menuManager = FindObjectOfType<CasualModeMenuManager>();
        if (menuManager != null)
        {
            menuManager.RefreshAllButtons();
        }
        else
        {
            Debug.LogWarning("<color=yellow>CasualModeMenuManager not found in scene - buttons not refreshed</color>");
        }
    }

    /// <summary>
    /// Print current progress for debugging
    /// </summary>
    [ContextMenu("DEBUG: Print Progress")]
    public void PrintProgress()
    {
        Debug.Log("<color=cyan>========== STAGE PROGRESS ==========</color>");
        Debug.Log($"Debug Mode: {unlockAllStagesForDebug}");
        Debug.Log($"Completed Stages: {string.Join(", ", completedStages.Select(x => (x + 1).ToString()))}");

        for (int i = 0; i < allStages.Length; i++)
        {
            bool completed = completedStages.Contains(i);
            bool unlocked = IsStageUnlocked(i);

            string status = completed ? "<color=green>COMPLETED</color>" :
                           (unlocked ? "<color=yellow>UNLOCKED</color>" : "<color=red>LOCKED</color>");

            Debug.Log($"Stage {i + 1}: {status}");
        }
        Debug.Log("<color=cyan>====================================</color>");
    }
}
