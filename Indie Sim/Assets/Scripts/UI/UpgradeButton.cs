using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UpgradeButton : MonoBehaviour
{
    [Header("Upgrade Settings")]
    public int upgradeCost = 100;

    [Header("References")]
    public Button button;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI insufficientCoinsText;

    [Header("Store Reference")]
    [SerializeField] private StoreManager storeManager;

    [Header("Insufficient Coins Settings")]
    public float messageDisplayDuration = 2f;

    private CoinManager coinManager;
    private bool isPurchased = false;

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        coinManager = FindObjectOfType<CoinManager>();

        if (coinManager == null)
        {
            Debug.LogError("CoinManager not found in the scene!");
        }

        if (insufficientCoinsText != null)
            insufficientCoinsText.gameObject.SetActive(false);

        if (storeManager == null)
        {
            storeManager = FindObjectOfType<StoreManager>();
        }

        UpdateCostDisplay();
        button.onClick.AddListener(CheckAndProcessPurchase);
        CheckAffordability();
    }

    void Update()
    {
        // ✅ FIX: Only check affordability if no upgrade has been selected yet
        if (!isPurchased && storeManager != null && !storeManager.IsUpgradeSelected())
        {
            CheckAffordability();
        }
    }

    void CheckAffordability()
    {
        if (coinManager == null || isPurchased)
            return;

        // ✅ FIX: Also check if store already has a selection
        if (storeManager != null && storeManager.IsUpgradeSelected())
        {
            button.interactable = false;
            return;
        }

        if (coinManager.GetCurrentCoins() >= upgradeCost)
        {
            button.interactable = true;
        }
        else
        {
            button.interactable = false;
        }
    }

    void CheckAndProcessPurchase()
    {
        if (coinManager == null || isPurchased)
            return;

        // ✅ FIX: Check if another upgrade was already selected
        if (storeManager != null && storeManager.IsUpgradeSelected())
        {
            Debug.Log("Another upgrade was already selected!");
            return;
        }

        if (coinManager.GetCurrentCoins() >= upgradeCost)
        {
            coinManager.SpendCoins(upgradeCost);
            isPurchased = true;
            button.interactable = false;

            if (costText != null)
                costText.text = "Purchased";

            Debug.Log("Upgrade purchased for " + upgradeCost + " coins!");

            // Notify store manager
            if (storeManager != null)
            {
                storeManager.OnUpgradeSelected();
            }

            GetComponent<UpgradePreviewDisplay>()?.OnUpgradePurchased();
        }
        else
        {
            StartCoroutine(ShowInsufficientCoinsMessage());
        }
    }

    IEnumerator ShowInsufficientCoinsMessage()
    {
        if (insufficientCoinsText != null)
        {
            insufficientCoinsText.gameObject.SetActive(true);
            yield return new WaitForSeconds(messageDisplayDuration);
            insufficientCoinsText.gameObject.SetActive(false);
        }
    }

    void UpdateCostDisplay()
    {
        if (costText != null)
            costText.text = upgradeCost + " Coins";
    }

    public void ResetButton()
    {
        isPurchased = false;
        CheckAffordability();
        if (costText != null)
            UpdateCostDisplay();
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(CheckAndProcessPurchase);
    }
}
