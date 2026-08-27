using UnityEngine;
using System;

/// <summary>
/// Tracks the run's score, derived from total damage dealt (Score = TotalDamageDealt / 10).
/// Mirrors CoinManager's singleton + event pattern so ScoreUI can subscribe the same way CoinUI does.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int _totalDamageDealt;
    private int _score;

    /// <summary>Fires whenever the displayed score changes. Argument = new score.</summary>
    public event Action<int> OnScoreChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public int GetScore() => _score;
    public int GetTotalDamageDealt() => _totalDamageDealt;

    /// <summary>
    /// Reports damage dealt by the player (bullets, stomp, etc.).
    /// Score is recomputed from the running damage total rather than adding
    /// damage/10 per hit, so remainders across many small hits aren't lost
    /// (e.g. five separate 4-damage hits should still add up to 2 score, not 0).
    /// </summary>
    public void AddDamage(int damage)
    {
        if (damage <= 0) return;

        _totalDamageDealt += damage;
        int newScore = _totalDamageDealt / 10;

        if (newScore != _score)
        {
            _score = newScore;
            OnScoreChanged?.Invoke(_score);
        }
    }
}
