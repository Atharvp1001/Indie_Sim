using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;

    [Header("UI Reference")]
    public TextMeshProUGUI coinText;

    private int currentCoins = 0;

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

    public void AddCoins(int amount)
    {
        currentCoins += amount;
        UpdateUI();
        Debug.Log($"Coins: {currentCoins}");
    }

    public int GetCoins()
    {
        return currentCoins;
    }

    private void UpdateUI()
    {
        if (coinText != null)
        {
            coinText.text = "Coins: " + currentCoins;
        }
    }
}
