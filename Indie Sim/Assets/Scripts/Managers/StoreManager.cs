using UnityEngine;
using UnityEngine.SceneManagement;

public class StoreManager : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("Name of the main menu scene")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("Name of the casual mode menu scene")]
    public string casualModeMenuSceneName = "CasualModeMenu";

    [Tooltip("Name of the casual mode gameplay scene")]
    public string casualModeSceneName = "CasualModeScene";

    [Header("References")]
    [Tooltip("Continue button for next level")]
    public GameObject continueButton;

    [Tooltip("Next Stage button for stage progression")]
    public GameObject nextStageButton;

    void Start()
    {
        // Ensure PersistentDataManager exists
        if (PersistentDataManager.Instance == null)
        {
            GameObject managerObj = new GameObject("PersistentDataManager");
            managerObj.AddComponent<PersistentDataManager>();
            Debug.LogWarning("PersistentDataManager created in Store scene");
        }

        Debug.Log("<color=cyan>Store Scene Loaded</color>");

        // Check if this is a stage transition or level transition
        UpdateButtonVisibility();

        PrintPlayerStatus();
    }

    /// <summary>
    /// Update which buttons should be visible
    /// </summary>
    void UpdateButtonVisibility()
    {
        if (PersistentDataManager.Instance == null) return;

        bool isStageTransition = PersistentDataManager.Instance.currentSession.isStageTransition;

        if (continueButton != null)
        {
            continueButton.SetActive(!isStageTransition); // Show for level transitions
        }

        if (nextStageButton != null)
        {
            nextStageButton.SetActive(isStageTransition); // Show for stage transitions
        }

        Debug.Log(isStageTransition ?
            "<color=yellow>Stage completed - showing Next Stage button</color>" :
            "<color=yellow>Level completed - showing Continue button</color>");
    }

    /// <summary>
    /// Return to Main Menu
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("<color=yellow>Returning to Main Menu...</color>");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Return to Casual Mode Menu (Stage Selection)
    /// </summary>
    public void ReturnToCasualModeMenu()
    {
        Debug.Log("<color=yellow>Returning to Casual Mode Menu...</color>");

        // Reset stage transition flag
        if (PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.currentSession.isStageTransition = false;
        }

        SceneManager.LoadScene(casualModeMenuSceneName);
    }

    /// <summary>
    /// Continue to next level (same stage)
    /// </summary>
    public void ContinueToNextLevel()
    {
        if (PersistentDataManager.Instance == null)
        {
            Debug.LogError("Cannot continue - PersistentDataManager missing!");
            return;
        }

        Debug.Log("<color=lime>Continuing to next level...</color>");
        SceneManager.LoadScene(casualModeSceneName);
    }

    /// <summary>
    /// Load next stage (after stage completion)
    /// </summary>
    public void LoadNextStage()
    {
        if (PersistentDataManager.Instance == null)
        {
            Debug.LogError("Cannot load next stage - PersistentDataManager missing!");
            return;
        }

        // Get current stage number
        int currentStageNum = PersistentDataManager.Instance.selectedStageConfig.stageNumber;
        int nextStageNum = currentStageNum + 1;

        Debug.Log($"<color=lime>Loading Stage {nextStageNum}...</color>");

        // Find the next stage config
        // For now, redirect to casual mode menu to select next stage
        // In the future, you could auto-load the next stage here
        PersistentDataManager.Instance.currentSession.isStageTransition = false;

        SceneManager.LoadScene(casualModeMenuSceneName);
    }

    /// <summary>
    /// Print player status for debugging
    /// </summary>
    void PrintPlayerStatus()
    {
        if (PersistentDataManager.Instance != null)
        {
            var session = PersistentDataManager.Instance.currentSession;
            Debug.Log("========== PLAYER STATUS ==========");
            Debug.Log($"Coins: {session.coins}");
            Debug.Log($"Health: {session.currentHealth}/{session.maxHealth}");
            Debug.Log($"Stage: {session.currentStageIndex}, Level: {session.currentLevelIndex}");
            Debug.Log($"Is Stage Transition: {session.isStageTransition}");
            Debug.Log("===================================");
        }
    }

    [ContextMenu("Print Player Status")]
    public void DebugPrintStatus()
    {
        PrintPlayerStatus();
    }
}
