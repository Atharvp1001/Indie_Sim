using UnityEngine;

public class CasualModeMenuManager : MonoBehaviour
{
    [Header("Stage Buttons")]
    public StageButton[] stageButtons;

    void Start()
    {
        // Buttons update themselves, but we can still refresh them here
        RefreshAllButtons();
    }

    /// <summary>
    /// Refresh all stage buttons (useful after unlocking or resetting)
    /// </summary>
    [ContextMenu("DEBUG: Refresh All Stage Buttons")]
    public void RefreshAllButtons()
    {
        // Find all StageButton components in the scene if not assigned
        if (stageButtons == null || stageButtons.Length == 0)
        {
            stageButtons = FindObjectsOfType<StageButton>();
            Debug.Log($"<color=yellow>Auto-found {stageButtons.Length} stage buttons in scene</color>");
        }

        Debug.Log("<color=cyan>Refreshing all stage buttons...</color>");

        foreach (StageButton btn in stageButtons)
        {
            if (btn != null)
            {
                btn.RefreshUnlockStatus();
            }
        }

        Debug.Log("<color=green>All stage buttons refreshed!</color>");
    }
}
