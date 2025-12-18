using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    // Singleton instance
    public static CoinManager Instance;

    [Header("UI Reference")]
    public TextMeshProUGUI coinText;

    [Header("Coin Tracking")]
    private int currentCoins = 0; // Coins for current run only

    // PlayerPrefs key for storing total coins
    private const string TOTAL_COINS_KEY = "TotalCoinsEverCollected";

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persists across scene changes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateUI();
    }

    /// <summary>
    /// Add coins to current run AND to total coins ever collected
    /// </summary>
    public void AddCoins(int amount)
    {
        // Add to current run
        currentCoins += amount;

        // Add to total coins ever collected (PlayerPrefs)
        int totalCoins = PlayerPrefs.GetInt(TOTAL_COINS_KEY, 0);
        totalCoins += amount;
        PlayerPrefs.SetInt(TOTAL_COINS_KEY, totalCoins);
        PlayerPrefs.Save(); // Save to disk

        UpdateUI();

        if (showDebugLogs)
        {
            Debug.Log($"Coins added: {amount} | Current Run: {currentCoins} | Total Ever: {totalCoins}");
        }

        // ** ACHIEVEMENT INTEGRATION **
        // Notify achievement manager to check if any coin achievements were unlocked
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.CheckCoinAchievements();
        }
        else
        {
            Debug.LogWarning("[CoinManager] AchievementManager not found. Achievement checks skipped.");
        }
    }

    /// <summary>
    /// Get coins from current run only
    /// </summary>
    public int GetCoins()
    {
        return currentCoins;
    }

    /// <summary>
    /// Spend coins from current run
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            UpdateUI();

            if (showDebugLogs)
            {
                Debug.Log($"Coins spent: {amount}. Remaining: {currentCoins}");
            }

            return true;
        }

        if (showDebugLogs)
        {
            Debug.LogWarning($"Not enough coins! Need: {amount}, Have: {currentCoins}");
        }

        return false;
    }

    /// <summary>
    /// Get current run coins
    /// </summary>
    public int GetCurrentCoins()
    {
        return currentCoins;
    }

    /// <summary>
    /// Check if player has enough coins in current run
    /// </summary>
    public bool HasEnoughCoins(int amount)
    {
        return currentCoins >= amount;
    }

    /// <summary>
    /// Get ALL coins ever collected (across all runs)
    /// Default value is 0 if never saved before
    /// </summary>
    public int GetTotalCoinsEverCollected()
    {
        return PlayerPrefs.GetInt(TOTAL_COINS_KEY, 0);
    }

    /// <summary>
    /// Reset current run coins (called when dungeon completes or game resets)
    /// Total coins are NOT reset - they persist forever
    /// </summary>
    public void ResetCurrentRunCoins()
    {
        currentCoins = 0;
        UpdateUI();

        if (showDebugLogs)
        {
            Debug.Log("[CoinManager] Current run coins reset. Total coins still saved.");
        }
    }

    /// <summary>
    /// DEBUG: Reset all coins (total and current)
    /// Use this for testing or if you want to start fresh
    /// </summary>
    public void DEBUG_ResetAllCoins()
    {
        currentCoins = 0;
        PlayerPrefs.DeleteKey(TOTAL_COINS_KEY);
        PlayerPrefs.Save();
        UpdateUI();

        if (showDebugLogs)
        {
            Debug.Log("[CoinManager] ALL coins reset (current and total)!");
        }
    }

    /// <summary>
    /// DEBUG: Print current coin status
    /// </summary>
    public void DEBUG_PrintCoinStatus()
    {
        int totalCoins = GetTotalCoinsEverCollected();
        Debug.Log($"========== COIN STATUS ==========");
        Debug.Log($"Current Run Coins: {currentCoins}");
        Debug.Log($"Total Coins Ever Collected: {totalCoins}");
        Debug.Log($"=================================");
    }

    /// <summary>
    /// Update the UI text display
    /// </summary>
    private void UpdateUI()
    {
        if (coinText != null)
        {
            // Show current run coins in UI
            coinText.text = "Coins: " + currentCoins;
        }
    }

    // Auto-save when application quits
    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    // Auto-save when application loses focus (for mobile/alt-tab)
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            PlayerPrefs.Save();
        }
    }
}
