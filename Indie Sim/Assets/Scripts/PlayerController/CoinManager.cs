using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    // Singleton instance
    public static CoinManager Instance;

    [Header("Coin Tracking")]
    private int currentCoins = 0; // Coins for current run only

    // PlayerPrefs key for storing total coins
    private const string TOTAL_COINS_KEY = "TotalCoinsEverCollected";

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Event for UI updates (other scripts can subscribe to this)
    public System.Action<int> OnCoinsChanged;

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

        // Notify subscribers (like UI) that coins changed
        OnCoinsChanged?.Invoke(currentCoins);

        if (showDebugLogs)
        {
            Debug.Log($"Coins added: {amount} | Current Run: {currentCoins} | Total Ever: {totalCoins}");
        }

        // ** ACHIEVEMENT INTEGRATION **
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.CheckCoinAchievements();
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
    /// Spend coins (returns true if successful)
    /// Also triggers coin loss achievements
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;

            // Notify subscribers
            OnCoinsChanged?.Invoke(currentCoins);

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
    /// </summary>
    public int GetTotalCoinsEverCollected()
    {
        return PlayerPrefs.GetInt(TOTAL_COINS_KEY, 0);
    }

    /// <summary>
    /// Reset current run coins (called when dungeon completes or game resets)
    /// </summary>
    public void ResetCurrentRunCoins()
    {
        currentCoins = 0;
        OnCoinsChanged?.Invoke(currentCoins);

        if (showDebugLogs)
        {
            Debug.Log("[CoinManager] Current run coins reset. Total coins still saved.");
        }
    }

    /// <summary>
    /// DEBUG: Reset all coins
    /// </summary>
    public void DEBUG_ResetAllCoins()
    {
        currentCoins = 0;
        PlayerPrefs.DeleteKey(TOTAL_COINS_KEY);
        PlayerPrefs.Save();
        OnCoinsChanged?.Invoke(currentCoins);

        if (showDebugLogs)
        {
            Debug.Log("[CoinManager] ALL coins reset!");
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

    // Auto-save when application quits
    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    // Auto-save when application pauses
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            PlayerPrefs.Save();
        }
    }
}
