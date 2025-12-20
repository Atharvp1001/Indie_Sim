using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;

    private void Start()
    {
        // Subscribe to coin changes
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinsChanged += UpdateCoinText;

            // Update immediately with current value
            UpdateCoinText(CoinManager.Instance.GetCurrentCoins());
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe when this UI is destroyed
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinsChanged -= UpdateCoinText;
        }
    }

    private void UpdateCoinText(int coins)
    {
        if (coinText != null)
        {
            coinText.text = "Coins: " + coins;
        }
    }
}
