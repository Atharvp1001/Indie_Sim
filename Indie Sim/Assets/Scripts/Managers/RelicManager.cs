using UnityEngine;

public class RelicManager : MonoBehaviour
{
    [Header("Relic Collection Settings")]
    [Tooltip("Total number of unique relics in the game (5-8)")]
    public int totalRelics = 8;

    [Tooltip("Coins given when player collects an already-found relic")]
    public int duplicateRelicCoins = 50;

    [Header("Current Run Status")]
    // Array to track which relics have been collected THIS RUN (not persistent)
    private bool[] relicsCollected;

    // Count of unique relics collected this run
    private int uniqueRelicsCollected = 0;

    // Current coin count
    private int coins = 0;

    private void Start()
    {
        // Initialize the array to track collected relics
        relicsCollected = new bool[totalRelics];

        // Set all relics as not collected at the start
        for (int i = 0; i < totalRelics; i++)
        {
            relicsCollected[i] = false;
        }

        Debug.Log("RelicManager initialized with " + totalRelics + " relics");
    }

    /// <summary>
    /// Called by the Relic script when player collects a relic
    /// </summary>
    public void CollectRelic(int relicIndex)
    {
        // Safety check: make sure the index is valid
        if (relicIndex < 0 || relicIndex >= totalRelics)
        {
            Debug.LogError("Invalid relic index: " + relicIndex);
            return;
        }

        // Check if this relic has been collected before in this run
        if (relicsCollected[relicIndex])
        {
            // Already collected - give coins instead
            GiveCoins(duplicateRelicCoins);
            Debug.Log("Duplicate relic " + relicIndex + " collected! Gave " + duplicateRelicCoins + " coins");
        }
        else
        {
            // New relic - mark it as collected
            relicsCollected[relicIndex] = true;
            uniqueRelicsCollected++;

            Debug.Log("New relic " + relicIndex + " collected! Total: " + uniqueRelicsCollected + "/" + totalRelics);

            // Check if player collected all relics
            if (uniqueRelicsCollected >= totalRelics)
            {
                OnAllRelicsCollected();
            }
        }
    }

    /// <summary>
    /// Gives coins to the player
    /// </summary>
    private void GiveCoins(int amount)
    {
        coins += amount;
        Debug.Log("Player received " + amount + " coins! Total coins: " + coins);

        // TODO: If you have a separate coin manager or UI, update it here
        // Example: CoinUI.Instance.UpdateCoins(coins);
    }

    /// <summary>
    /// Called when all relics have been collected
    /// </summary>
    private void OnAllRelicsCollected()
    {
        Debug.Log("All relics collected! Player wins!");
        // TODO: Add your win condition logic here
        // Example: trigger end screen, give bonus, etc.
    }

    /// <summary>
    /// Check if a specific relic has been collected
    /// </summary>
    public bool IsRelicCollected(int relicIndex)
    {
        if (relicIndex < 0 || relicIndex >= totalRelics)
            return false;

        return relicsCollected[relicIndex];
    }

    /// <summary>
    /// Get the number of unique relics collected this run
    /// </summary>
    public int GetRelicsCollectedCount()
    {
        return uniqueRelicsCollected;
    }

    /// <summary>
    /// Get current coin count
    /// </summary>
    public int GetCoins()
    {
        return coins;
    }

    /// <summary>
    /// Reset all relics for a new run (call this when starting a new game)
    /// </summary>
    public void ResetRelics()
    {
        for (int i = 0; i < totalRelics; i++)
        {
            relicsCollected[i] = false;
        }
        uniqueRelicsCollected = 0;
        Debug.Log("Relics reset for new run");
    }
}
