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
        Time.timeScale = 1f;

        // ✅ Always go to main menu — full reset regardless of which scene we're in
        GameManager.Instance.LoadMenu();
    }
}
