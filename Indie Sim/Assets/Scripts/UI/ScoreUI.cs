using UnityEngine;
using TMPro;

/// <summary>
/// Displays ScoreManager's current score on the HUD. Same subscribe/refresh pattern as CoinUI.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;

    private void Start()
    {
        if (ScoreManager.Instance == null) return;

        ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
        RefreshUI(ScoreManager.Instance.GetScore());
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance == null) return;
        ScoreManager.Instance.OnScoreChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int score) => RefreshUI(score);

    private void RefreshUI(int score)
    {
        if (scoreText != null)
            scoreText.text = $"SCORE: {score}";
    }
}
