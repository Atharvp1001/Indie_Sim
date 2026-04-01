using UnityEngine;
using System;

/// <summary>
/// Manages the player's coin economy for a single run.
/// 
/// Provides:
///   - OnCoinsChanged event      → CoinUI subscribes to update the HUD
///   - GetCurrentCoins()         → UpgradeButtonUI, StoreManager use this
///   - SpendCoins()              → StoreManager calls this on purchase
///   - HasEnoughCoins()          → PlayerStompController uses this
///   - GetCoinsCollectedThisRun()→ StatTracker uses this for end screen
///   - GetTotalCoinsEverCollected() → PlayerHealth uses this (lifetime stat)
///   - ResetForNewRun()          → RoguelikeManager calls this at run start
/// </summary>
public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("Starting Balance")]
    [SerializeField] private int startingCoins = 0;

    [Header("Coin Cap")]
    [SerializeField] private int startingMaxCoins = 200;

    // ─────────────────────────────────────────────────────────────────
    //  STATE
    // ─────────────────────────────────────────────────────────────────
    private int _currentCoins;
    private int _coinsCollectedThisRun;
    private int _totalCoinsEverCollected; // persists across runs in memory
    private int _maxCoins;
    // ─────────────────────────────────────────────────────────────────
    //  EVENTS
    //  Subscribe: CoinManager.Instance.OnCoinsChanged += MyMethod;
    //  Unsubscribe in OnDestroy to avoid memory leaks.
    //  Passes the new coin count so listeners don't need to call Get().
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Fires whenever coins change. Argument = new current balance.</summary>
    public event Action<int> OnCoinsChanged;
    /// <summary>Fires when the coin cap increases (e.g. Coin Purse upgrade).</summary>
    public event Action<int> OnMaxCoinsChanged;
    // ─────────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
        InitialiseRun();
    }

    private void InitialiseRun()
    {
        _currentCoins = startingCoins;
        _coinsCollectedThisRun = 0;
        _maxCoins = startingMaxCoins;
        // _totalCoinsEverCollected intentionally NOT reset
        Debug.Log($"[CoinManager] Initialised — {_currentCoins}/{_maxCoins}");
    }

    private void Start()
    {
        //ResetForNewRun();
    }

    // ─────────────────────────────────────────────────────────────────
    //  PUBLIC API — GETTERS
    // ─────────────────────────────────────────────────────────────────

    public int GetCurrentCoins() => _currentCoins;

    /// <summary>Coins collected since the run began (for StatTracker end screen).</summary>
    public int GetCoinsCollectedThisRun() => _coinsCollectedThisRun;

    /// <summary>Lifetime coins across all runs (for PlayerHealth death screen).</summary>
    public int GetTotalCoinsEverCollected() => _totalCoinsEverCollected;

    /// <summary>Returns true if the player can afford the given amount.</summary>
    public bool HasEnoughCoins(int amount) => _currentCoins >= amount;

    public int MaxCoins => _maxCoins;

    public int GetMaxCoins() => _maxCoins;


    // ─────────────────────────────────────────────────────────────────
    //  PUBLIC API — MUTATORS
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Add coins (e.g. enemy drop, pickup).
    /// amount must be positive.
    /// </summary>
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        int actual = Mathf.Min(amount, _maxCoins - _currentCoins); // ← clamp to cap
        if (actual <= 0) return;

        _currentCoins += actual;
        _coinsCollectedThisRun += actual;
        _totalCoinsEverCollected += actual;

        Debug.Log($"[CoinManager] +{actual} coins → {_currentCoins}/{_maxCoins}");
        OnCoinsChanged?.Invoke(_currentCoins);
    }

    /// <summary>
    /// Spend coins (upgrade purchase, stomp cost, etc.).
    /// Returns true if successful, false if not enough coins.
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return true;

        if (!HasEnoughCoins(amount))
        {
            Debug.LogWarning($"[CoinManager] Not enough coins! Have {_currentCoins}, need {amount}");
            return false;
        }

        _currentCoins -= amount;
        Debug.Log($"[CoinManager] -{amount} coins → {_currentCoins} remaining");
        OnCoinsChanged?.Invoke(_currentCoins);
        return true;
    }

    /// <summary>
    /// Increase the maximum coin cap. Called by UpgradeManager when a
    /// CoinPurse upgrade is applied.
    /// </summary>
    public void IncreaseMaxCoins(int amount)
    {
        if (amount <= 0) return;
        _maxCoins += amount;
        Debug.Log($"[CoinManager] Max coins increased to {_maxCoins}");
        OnMaxCoinsChanged?.Invoke(_maxCoins);
        OnCoinsChanged?.Invoke(_currentCoins); // refresh UI fill bar
    }


    /// <summary>
    /// Call at the start of each new run to reset per-run counters.
    /// Lifetime total is NOT reset.
    /// </summary>
    public void ResetForNewRun()
    {
        _currentCoins = startingCoins;
        _coinsCollectedThisRun = 0;
        _maxCoins = startingMaxCoins;  // ← add this line

        Debug.Log($"[CoinManager] Run reset — {_currentCoins}/{_maxCoins}");
        OnCoinsChanged?.Invoke(_currentCoins);
        OnMaxCoinsChanged?.Invoke(_maxCoins);
    }
}