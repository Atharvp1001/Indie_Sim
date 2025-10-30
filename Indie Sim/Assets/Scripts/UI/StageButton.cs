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
    [Tooltip("Is this stage unlocked and playable?")]
    public bool isUnlocked = true;

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
    private bool isInitialized = false;

    void Start()
    {
        button = GetComponent<Button>();

        // Validate stage config
        if (stageConfig == null)
        {
            Debug.LogError($"StageButton on {gameObject.name}: No StageConfigSO assigned!");
            button.interactable = false;
            return;
        }

        // Setup button click listener
        button.onClick.AddListener(OnStageButtonClicked);

        // Update visual state
        UpdateButtonVisuals();

        isInitialized = true;
    }

    /// <summary>
    /// Called when the stage button is clicked
    /// </summary>
    void OnStageButtonClicked()
    {
        if (!isUnlocked)
        {
            Debug.LogWarning($"Stage {stageConfig.stageNumber} is locked!");
            ShowLockedMessage();
            return;
        }

        Debug.Log($"<color=cyan>Loading Stage {stageConfig.stageNumber}</color>");

        // Make sure PersistentDataManager exists
        if (PersistentDataManager.Instance == null)
        {
            Debug.LogError("PersistentDataManager not found! Make sure it exists in the scene.");
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
    void UpdateButtonVisuals()
    {
        // Update button interactability
        button.interactable = isUnlocked;

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
        if (stageNumberText != null)
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
            Debug.LogWarning($"Stage {stageConfig.stageNumber} is already unlocked!");
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
            Debug.LogWarning($"Stage {stageConfig.stageNumber} is already locked!");
            return;
        }

        isUnlocked = false;
        UpdateButtonVisuals();

        Debug.Log($"<color=red>Stage {stageConfig.stageNumber} locked!</color>");
    }

    /// <summary>
    /// Show a message when player tries to click locked stage
    /// </summary>
    void ShowLockedMessage()
    {
        Debug.Log($"<color=orange>Stage {stageConfig.stageNumber} is locked! Complete previous stages to unlock.</color>");

        // TODO: You can add a UI popup here later
        // For now, just log it
    }

    /// <summary>
    /// Get stage information
    /// </summary>
    public int GetStageNumber()
    {
        return stageConfig.stageNumber;
    }

    public int GetNumberOfLevels()
    {
        return stageConfig.numberOfLevels;
    }
}
