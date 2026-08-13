using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to the Retry button in the Canvas.
/// Automatically handles both dungeon and boss scene correctly.
/// </summary>
public class RetryButton : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "Main Menu";
    //[SerializeField] private string dungeonSceneName = "DungeonScene";

    /// <summary>
    /// Wire this to the button's OnClick in the Inspector
    /// </summary>
    public void OnRetryClicked()
    {
        Debug.Log($"[RetryButton] OnRetryClicked() on {gameObject.name} in scene '{gameObject.scene.name}'.");
        Time.timeScale = 1f;

        // Restarts the run in place — works identically whether death
        // happened in RoguelikeMode or BossArena (D2).
        GameManager.Instance.RetryRun();
    }
}
