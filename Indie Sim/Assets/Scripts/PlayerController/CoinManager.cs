using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;

    [Header("UI Reference")]
    public TextMeshProUGUI coinText;

    // Current run coins
    private int currentCoins = 0;

    // PlayerPrefs key for storing total coins
    private const string TOTAL_COINS_KEY = "TotalCoinsEverCollected";

    private void Awake()
    {
        // Simple singleton
        if (Instance == null)
        {
            Instance = this;
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
        Debug.Log($"Current Run Coins: {currentCoins} | Total Coins Ever: {totalCoins}");
    }

    /// <summary>
    /// Get coins from current run only
    /// </summary>
    public int GetCoins()
    {
        return currentCoins;
    }

    public bool SpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            Debug.Log("Coins spent: " + amount + ". Remaining: " + currentCoins);
            return true;
        }
        return false;
    }

    public int GetCurrentCoins()
    {
        return currentCoins;
    }

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
        Debug.Log("[CoinManager] Current run coins reset. Total coins still saved.");
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
        Debug.Log("[CoinManager] ALL coins reset (current and total)!");
    }

    /// <summary>
    /// DEBUG: Print current coin status
    /// </summary>
    public void DEBUG_PrintCoinStatus()
    {
        int totalCoins = GetTotalCoinsEverCollected();
        Debug.Log($"========== COIN-STATUS ==========");
        Debug.Log($"Current Run Coins: {currentCoins}");
        Debug.Log($"Total Coins Ever Collected: {totalCoins}");
        Debug.Log($"================================");
    }

    private void UpdateUI()
    {
        if (coinText != null)
        {
            // Show current run coins in UI
            coinText.text = "Coins: " + currentCoins;
        }
    }
}
