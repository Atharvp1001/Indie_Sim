using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lives in the Boss scene. Handles all boss scene specific logic —
/// victory screen, boss death, game over, etc.
/// </summary>
public class BossSceneManager : MonoBehaviour
{
    [Header("Victory Settings")]
    [SerializeField] private string victoryPanelName = "VictoryPanel"; // Must match name in Canvas

    private GameObject victoryPanel;
    private Canvas persistedCanvas;

    private void Start()
    {
        FindPersistedCanvas();
    }

    private void FindPersistedCanvas()
    {
        // Canvas came from DontDestroyOnLoad — FindObjectOfType finds it fine
        persistedCanvas = FindObjectOfType<Canvas>();

        if (persistedCanvas != null)
        {
            Transform panel = persistedCanvas.transform.Find(victoryPanelName);
            if (panel != null)
            {
                victoryPanel = panel.gameObject;
                victoryPanel.SetActive(false);
                Debug.Log("[BossSceneManager] Victory panel found and ready");
            }
            else
            {
                Debug.LogWarning($"[BossSceneManager] '{victoryPanelName}' not found in Canvas!");
            }
        }
        else
        {
            Debug.LogWarning("[BossSceneManager] No persisted Canvas found!");
        }
    }

    /// <summary>
    /// Call this when the boss dies
    /// </summary>
    public void OnBossDefeated()
    {
        Debug.Log("<color=lime>[BossSceneManager] Boss defeated!</color>");

        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        Time.timeScale = 0f; // Pause game
    }
}
