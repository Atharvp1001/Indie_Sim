using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to the Retry button in the Canvas.
/// Automatically handles both dungeon and boss scene correctly.
/// </summary>
public class RetryButton : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    //[SerializeField] private string dungeonSceneName = "DungeonScene";

    /// <summary>
    /// Wire this to the button's OnClick in the Inspector
    /// </summary>
    public void OnRetryClicked()
    {
        Time.timeScale = 1f;
        DestroyPersistedObjects();

        // ✅ Always go to main menu — full reset regardless of which scene we're in
        SceneManager.LoadScene("Main Menu");
    }

    private void DestroyPersistedObjects()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) Destroy(player);

        // Canvas destroys itself since this script is on it
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null) Destroy(canvas.gameObject);

        CoinManager coin = FindObjectOfType<CoinManager>();
        if (coin != null) Destroy(coin.gameObject);

        UpgradeManager upgrade = FindObjectOfType<UpgradeManager>();
        if (upgrade != null) Destroy(upgrade.gameObject);

        MusicManager music = FindObjectOfType<MusicManager>();
        if (music != null) Destroy(music.gameObject);

        EnemyKillTracker killTracker = FindObjectOfType<EnemyKillTracker>();
        if (killTracker != null) Destroy(killTracker.gameObject);

        CustomCrosshair crosshair = FindObjectOfType<CustomCrosshair>();    
        if (crosshair != null) Destroy(crosshair.gameObject);

    }
}
