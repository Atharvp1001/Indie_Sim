using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
/*
public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance { get; private set; }

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";
    public string casualModeMenuSceneName = "CasualModeMenu";
    public string casualModeSceneName = "CasualModeScene";

    [Header("UI References")]
    public Button continueButton;
    public Button nextStageButton;
    public Button menuButton;

    [Header("Upgrade Panel")]
    public GameObject upgradePanel; // ← Panel with all upgrade buttons

    [Header("Debug Settings")]
    public bool debugMode = true;
    [SerializeField] private bool showDebugButtons = true;

    [Header("Debug Buttons")]
    public Button resetUpgradesButton;
    public Button resetFlagsButton;
    public Button resetAllButton;

    // Track completed stage/level combinations
    private HashSet<string> completedLevels = new HashSet<string>();
    private const string COMPLETED_LEVELS_KEY = "CompletedLevels";

    void Awake()
    {
        // Simple singleton
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate StoreManager destroyed!");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

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

        // Load completed levels from PlayerPrefs
        LoadCompletedLevels();

        // Check if upgrades should be shown
        CheckStageLevelIsDone();

        SetupButtons();
        SetupDebugButtons();
        UpdateButtonVisibility();

        if (debugMode)
        {
            PrintPlayerStatus();
            PrintUpgradeStatus();
            PrintCompletedLevels();
        }
    }

    void OnDestroy()
    {
        // Cleanup singleton reference
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("<color=yellow>StoreManager cleaned up</color>");
        }

        RemoveAllListeners();
    }

    // ========== COMPLETED LEVELS TRACKING ==========

    /// <summary>
    /// Load completed levels from PlayerPrefs
    /// Format: "Stage_0_Level_1,Stage_0_Level_2,Stage_1_Level_1"
    /// </summary>
    void LoadCompletedLevels()
    {
        string data = PlayerPrefs.GetString(COMPLETED_LEVELS_KEY, "");

        if (!string.IsNullOrEmpty(data))
        {
            string[] levels = data.Split(',');
            foreach (string level in levels)
            {
                if (!string.IsNullOrEmpty(level))
                {
                    completedLevels.Add(level);
                }
            }

            Debug.Log($"<color=cyan>Loaded {completedLevels.Count} completed levels</color>");
        }
    }

    /// <summary>
    /// Save completed levels to PlayerPrefs
    /// </summary>
    void SaveCompletedLevels()
    {
        string data = string.Join(",", completedLevels);
        PlayerPrefs.SetString(COMPLETED_LEVELS_KEY, data);
        PlayerPrefs.Save();

        Debug.Log($"<color=green>✓ Saved {completedLevels.Count} completed levels</color>");
    }

    /// <summary>
    /// Check if the current stage/level has been completed before
    /// If NOT completed before → Show upgrades
    /// If completed before → Hide upgrades
    /// </summary>
    void CheckStageLevelIsDone()
    {
        string currentLevelKey = GetCurrentLevelKey();

        if (!completedLevels.Contains(currentLevelKey))
        {
            // First time completing this level
            Debug.Log($"<color=green>First time completing: {currentLevelKey} - SHOWING UPGRADES</color>");
            ShowUpgrades();

            // Mark as completed
            completedLevels.Add(currentLevelKey);
            SaveCompletedLevels();
        }
        else
        {
            // Already completed this level before
            Debug.Log($"<color=orange>Already completed: {currentLevelKey} - HIDING UPGRADES</color>");
            HideUpgrades();
        }
    }

    /// <summary>
    /// Show the upgrade panel
    /// </summary>
    void ShowUpgrades()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
            Debug.Log("<color=lime>✓ Upgrade panel shown</color>");
        }
        else
        {
            Debug.LogError("Upgrade panel not assigned!");
        }
    }

    /// <summary>
    /// Hide the upgrade panel
    /// </summary>
    void HideUpgrades()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
            Debug.Log("<color=red>✓ Upgrade panel hidden (level already completed)</color>");
        }
    }

    /// <summary>
    /// Get current level key: "Stage_X_Level_Y"
    /// </summary>
    string GetCurrentLevelKey()
    {
        int stage = 0;
        int level = 0;

        if (PersistentDataManager.Instance != null)
        {
            stage = PersistentDataManager.Instance.currentSession.currentStageIndex;
            level = PersistentDataManager.Instance.currentSession.currentLevelIndex;
        }

        return $"Stage_{stage}_Level_{level}";
    }

    // ========== BUTTON SETUP ==========

    void SetupButtons()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueToNextLevel);
        else
            Debug.LogWarning("Continue button not assigned!");

        if (nextStageButton != null)
            nextStageButton.onClick.AddListener(LoadNextStage);
        else
            Debug.LogWarning("Next Stage button not assigned!");

        if (menuButton != null)
            menuButton.onClick.AddListener(ReturnToCasualModeMenu);
        else
            Debug.LogWarning("Menu button not assigned!");
    }

    void SetupDebugButtons()
    {
        if (!showDebugButtons) return;

        if (resetUpgradesButton != null)
        {
            resetUpgradesButton.onClick.AddListener(DebugResetUpgrades);
            resetUpgradesButton.gameObject.SetActive(debugMode);
        }

        if (resetFlagsButton != null)
        {
            resetFlagsButton.onClick.AddListener(DebugResetFlags);
            resetFlagsButton.gameObject.SetActive(debugMode);
        }

        if (resetAllButton != null)
        {
            resetAllButton.onClick.AddListener(DebugResetAll);
            resetAllButton.gameObject.SetActive(debugMode);
        }
    }

    void RemoveAllListeners()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(ContinueToNextLevel);

        if (nextStageButton != null)
            nextStageButton.onClick.RemoveListener(LoadNextStage);

        if (menuButton != null)
            menuButton.onClick.RemoveListener(ReturnToCasualModeMenu);

        if (resetUpgradesButton != null)
            resetUpgradesButton.onClick.RemoveListener(DebugResetUpgrades);

        if (resetFlagsButton != null)
            resetFlagsButton.onClick.RemoveListener(DebugResetFlags);

        if (resetAllButton != null)
            resetAllButton.onClick.RemoveListener(DebugResetAll);
    }

    void UpdateButtonVisibility()
    {
        if (PersistentDataManager.Instance == null) return;

        bool isStageTransition = PersistentDataManager.Instance.currentSession.isStageTransition;

        if (continueButton != null)
            continueButton.gameObject.SetActive(!isStageTransition);

        if (nextStageButton != null)
            nextStageButton.gameObject.SetActive(isStageTransition);

        Debug.Log(isStageTransition ?
            "<color=yellow>Stage completed - showing Next Stage button</color>" :
            "<color=yellow>Level completed - showing Continue button</color>");
    }

    // ========== SCENE NAVIGATION ==========

    public void ReturnToMainMenu()
    {
        Debug.Log("<color=yellow>Returning to Main Menu...</color>");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ReturnToCasualModeMenu()
    {
        Debug.Log("<color=yellow>Returning to Casual Mode Menu...</color>");

        if (PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.currentSession.isStageTransition = false;
        }

        SceneManager.LoadScene(casualModeMenuSceneName);
    }

    public void ContinueToNextLevel()
    {
        if (PersistentDataManager.Instance == null)
        {
            Debug.LogError("Cannot continue - PersistentDataManager missing!");
            return;
        }

        Debug.Log($"<color=lime>Continuing to next level...</color>");
        SceneManager.LoadScene(casualModeSceneName);
    }

    public void LoadNextStage()
    {
        if (PersistentDataManager.Instance == null)
        {
            Debug.LogError("Cannot load next stage - PersistentDataManager missing!");
            return;
        }

        int currentStageNum = PersistentDataManager.Instance.selectedStageConfig.stageNumber;
        int nextStageNum = currentStageNum + 1;

        Debug.Log($"<color=lime>Loading Stage {nextStageNum}...</color>");

        PersistentDataManager.Instance.currentSession.isStageTransition = false;
        SceneManager.LoadScene(casualModeMenuSceneName);
    }

    // ========== DEBUG METHODS ==========

    void DebugResetUpgrades()
    {
        Debug.Log("<color=red>╔════════════════════════════════════════╗</color>");
        Debug.Log("<color=red>║  DEBUG: RESETTING ALL UPGRADE BONUSES  ║</color>");
        Debug.Log("<color=red>╚════════════════════════════════════════╝</color>");

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.ResetAllUpgrades();
        }
        else
        {
            Debug.LogError("UpgradeManager not found!");
        }

        Debug.Log("<color=green>✓ All upgrades reset to 0</color>");
    }

    void DebugResetFlags()
    {
        Debug.Log("<color=orange>╔════════════════════════════════════════╗</color>");
        Debug.Log("<color=orange>║    DEBUG: CLEARING COMPLETED LEVELS    ║</color>");
        Debug.Log("<color=orange>╚════════════════════════════════════════╝</color>");

        completedLevels.Clear();
        PlayerPrefs.DeleteKey(COMPLETED_LEVELS_KEY);
        PlayerPrefs.Save();

        Debug.Log("<color=green>✓ All completed level flags cleared</color>");

        // Re-check and show upgrades
        CheckStageLevelIsDone();
    }

    void DebugResetAll()
    {
        Debug.Log("<color=magenta>╔════════════════════════════════════════╗</color>");
        Debug.Log("<color=magenta>║      DEBUG: FULL RESET - EVERYTHING    ║</color>");
        Debug.Log("<color=magenta>╚════════════════════════════════════════╝</color>");

        DebugResetUpgrades();
        Debug.Log("");
        DebugResetFlags();

        Debug.Log("<color=magenta>✓ COMPLETE RESET FINISHED</color>");
    }

    // ========== PRINT METHODS ==========

    void PrintPlayerStatus()
    {
        if (PersistentDataManager.Instance != null)
        {
            var session = PersistentDataManager.Instance.currentSession;
            Debug.Log("<color=cyan>========== PLAYER STATUS ==========</color>");
            Debug.Log($"<color=yellow>Coins: {session.coins}</color>");
            Debug.Log($"<color=yellow>Health: {session.currentHealth}/{session.maxHealth}</color>");
            Debug.Log($"<color=yellow>Stage: {session.currentStageIndex}, Level: {session.currentLevelIndex}</color>");
            Debug.Log($"<color=yellow>Is Stage Transition: {session.isStageTransition}</color>");
            Debug.Log("<color=cyan>===================================</color>");
        }
    }

    void PrintUpgradeStatus()
    {
        if (UpgradeManager.Instance == null) return;

        Debug.Log("<color=yellow>========== UPGRADE STATUS ==========</color>");
        Debug.Log($"<color=lime>Health Bonus: +{UpgradeManager.Instance.healthBonus}</color>");
        Debug.Log($"<color=lime>Speed Bonus: +{UpgradeManager.Instance.speedBonus:F1}</color>");
        Debug.Log($"<color=lime>Pistol Damage: +{UpgradeManager.Instance.GetWeaponDamageBonus("Pistol")}</color>");
        Debug.Log($"<color=lime>MachineGun Damage: +{UpgradeManager.Instance.GetWeaponDamageBonus("MachineGun")}</color>");
        Debug.Log($"<color=lime>Shotgun Damage: +{UpgradeManager.Instance.GetWeaponDamageBonus("Shotgun")}</color>");
        Debug.Log("<color=yellow>====================================</color>");
    }

    void PrintCompletedLevels()
    {
        Debug.Log("<color=magenta>========== COMPLETED LEVELS ==========</color>");
        Debug.Log($"<color=cyan>Total: {completedLevels.Count}</color>");

        foreach (string level in completedLevels)
        {
            Debug.Log($"<color=cyan>  • {level}</color>");
        }

        Debug.Log("<color=magenta>======================================</color>");
    }

    [ContextMenu("Print Player Status")]
    public void DebugPrintStatus()
    {
        PrintPlayerStatus();
    }

    [ContextMenu("Print Upgrade Status")]
    public void DebugPrintUpgradeStatus()
    {
        PrintUpgradeStatus();
    }

    [ContextMenu("Print Completed Levels")]
    public void DebugPrintCompletedLevels()
    {
        PrintCompletedLevels();
    }
}
*/