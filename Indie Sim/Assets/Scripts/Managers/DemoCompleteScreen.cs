using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Scene-local, lives in BossArena. Shown by GameManager.CompleteRun() when
/// the boss is defeated (D2 terminal success path). Its only exit returns to
/// Main Menu — there is no loop back to RoguelikeMode. Kept plain (a state,
/// not a feature) per the plan; polish later.
/// </summary>
public class DemoCompleteScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    /// <summary>
    /// Call after GameSession.EndRun() has already saved, so lifetime stats
    /// shown here are accurate.
    /// </summary>
    public void Show()
    {
        if (summaryText != null)
            summaryText.text = BuildSummary();

        if (panel != null)
            panel.SetActive(true);

        Time.timeScale = 0f;
    }

    // Reads straight from the still-persistent (DontDestroyOnLoad) managers,
    // same as StatTracker's death-screen summary — GameSession.CurrentRun's
    // equivalent fields aren't live-written yet (that's Phase 6).
    private string BuildSummary()
    {
        int kills = EnemyKillTracker.Instance != null ? EnemyKillTracker.Instance.GetKillsThisRun() : 0;
        int coins = CoinManager.Instance != null ? CoinManager.Instance.GetCoinsCollectedThisRun() : 0;
        int dungeonsCleared = GameSession.Instance != null ? GameSession.Instance.CurrentRun.DungeonsClearedThisRun : 0;

        return $"Kills: {kills}\nCoins: {coins}\nDungeons Cleared: {dungeonsCleared}";
    }

    private void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        GameManager.Instance.ReturnToMainMenu();
    }
}
