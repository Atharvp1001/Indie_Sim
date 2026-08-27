using UnityEngine;
using TMPro;

/// <summary>
/// Displays ScoreManager's current score on the HUD. Same subscribe/refresh pattern as CoinUI.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI scoreTextShadow;

    
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
        // Zero-padded, minimum 5 digits: 0 -> "00000", 25 -> "00025",
        // 123456 -> "123456" (grows past 5 when needed).
        string text = Mathf.Max(0, score).ToString("D5");

        if (scoreText != null)
            scoreText.text = text;
        if(scoreTextShadow != null )
            scoreTextShadow.text = text;
    }
}
