using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[RequireComponent(typeof(Button))]
public class StageButton : MonoBehaviour
{
    [Header("Stage Configuration")]
    [Tooltip("Drag the Stage ScriptableObject for this button here")]
    public StageConfigSO stageConfig;

    [Header("Scene Settings")]
    [Tooltip("Name of the casual mode gameplay scene")]
    public string casualModeSceneName = "CasualModeScene";

    [Header("Button Settings")]
    [Tooltip("Stage index (0-based) - auto-set from stageConfig")]
    public int stageIndex = -1;

    [Tooltip("Is this stage unlocked and playable? (Set automatically on Start)")]
    public bool isUnlocked = false;

    [Header("Visual Feedback (Optional)")]
    [Tooltip("Overlay that shows when stage is locked")]
    public GameObject lockedOverlay;

    [Tooltip("Button image for color changes")]
    public Image buttonImage;

    [Tooltip("Color when stage is unlocked")]
    public Color unlockedColor = Color.white;

    [Tooltip("Color when stage is locked")]
    public Color lockedColor = Color.gray;

    [Header("Text Display (Optional)")]
    [Tooltip("Text to show stage number")]
    public TextMeshProUGUI stageNumberText;

    [Tooltip("Text to show lock status")]
    public TextMeshProUGUI lockStatusText;

    private Button button;

    void Start()
    {
        button = GetComponent<Button>();

        // Validate stage config
        if (stageConfig == null)
        {
            Debug.LogError($"<color=red>StageButton on {gameObject.name}: No StageConfigSO assigned!</color>");
            button.interactable = false;
            return;
        }

        // Calculate stage index from stageConfig
        stageIndex = stageConfig.stageNumber - 1; // Convert to 0-based index

        // Check unlock status from StageUnlockManager
        CheckUnlockStatus();

        // Setup button click listener
        button.onClick.AddListener(OnStageButtonClicked);

        // Update visual state
        UpdateButtonVisuals();
    }

    /// <summary>
    /// Check if this stage is unlocked from StageUnlockManager
    /// </summary>
    void CheckUnlockStatus()
    {
        if (StageUnlockManager.Instance != null)
        {
            isUnlocked = StageUnlockManager.Instance.IsStageUnlocked(stageIndex);
            Debug.Log($"<color=cyan>Stage {stageIndex + 1} unlock check: {(isUnlocked ? "UNLOCKED" : "LOCKED")}</color>");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>StageUnlockManager not found! Defaulting Stage {stageIndex + 1} to locked.</color>");
            isUnlocked = false;
        }
    }

    /// <summary>
    /// Called when the stage button is clicked
    /// </summary>
    void OnStageButtonClicked()
    {
        if (!isUnlocked)
        {
            ShowLockedMessage();
            return;
        }

        Debug.Log($"<color=cyan>Loading Stage {stageConfig.stageNumber}...</color>");

        // Make sure PersistentDataManager exists
        if (PersistentDataManager.Instance == null)
        {
            Debug.LogError("<color=red>PersistentDataManager not found! Make sure it exists in the scene.</color>");
            return;
        }

        // Store the selected stage in the persistent manager
        PersistentDataManager.Instance.SetSelectedStage(stageConfig);

        // Reset session data for new stage
        PersistentDataManager.Instance.StartNewRun();

        Debug.Log($"<color=yellow>Transitioning to Stage {stageConfig.stageNumber}...</color>");

        // Load the casual mode scene
        SceneManager.LoadScene(casualModeSceneName);
    }

    /// <summary>
    /// Update button visual elements based on lock state
    /// </summary>
    public void UpdateButtonVisuals()
    {
        if (button == null) // Safety check
            button = GetComponent<Button>();

        // Update button interactability
        if (button != null)
        {
            button.interactable = isUnlocked;
        }

        // Update locked overlay visibility
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked);
        }

        // Update button image color
        if (buttonImage != null)
        {
            buttonImage.color = isUnlocked ? unlockedColor : lockedColor;
        }

        // Update text displays
        if (stageNumberText != null && stageConfig != null)
        {
            stageNumberText.text = $"Stage {stageConfig.stageNumber}";
        }

        if (lockStatusText != null)
        {
            lockStatusText.text = isUnlocked ? "Unlocked" : "Locked";
            lockStatusText.color = isUnlocked ? Color.green : Color.red;
        }
    }

    /// <summary>
    /// Unlock this stage and update visuals
    /// </summary>
    public void UnlockStage()
    {
        if (isUnlocked)
        {
            return;
        }

        isUnlocked = true;
        UpdateButtonVisuals();

        Debug.Log($"<color=lime>Stage {stageConfig.stageNumber} unlocked!</color>");
    }

    /// <summary>
    /// Lock this stage and update visuals
    /// </summary>
    public void LockStage()
    {
        if (!isUnlocked)
        {
            return;
        }

        isUnlocked = false;
        UpdateButtonVisuals();

        Debug.Log($"<color=red>Stage {stageConfig.stageNumber} locked!</color>");
    }

    /// <summary>
    /// Refresh unlock status from StageUnlockManager
    /// </summary>
    public void RefreshUnlockStatus()
    {
        CheckUnlockStatus();
        UpdateButtonVisuals();
    }

    /// <summary>
    /// Show a message when player tries to click locked stage
    /// </summary>
    void ShowLockedMessage()
    {
        Debug.Log($"<color=orange>Stage {stageConfig.stageNumber} is locked! Complete previous stages to unlock.</color>");

        // TODO: You can add a UI popup here later
        // Example: lockedMessagePanel.SetActive(true);
    }

    /// <summary>
    /// Get stage information
    /// </summary>
    public int GetStageNumber()
    {
        return stageConfig != null ? stageConfig.stageNumber : -1;
    }

    public int GetNumberOfLevels()
    {
        return stageConfig != null ? stageConfig.numberOfLevels : 0;
    }

    /// <summary>
    /// Force refresh the button visuals (useful for editor or runtime changes)
    /// </summary>
    [ContextMenu("Update Visuals")]
    public void ForceUpdateVisuals()
    {
        CheckUnlockStatus();
        UpdateButtonVisuals();
    }
}
