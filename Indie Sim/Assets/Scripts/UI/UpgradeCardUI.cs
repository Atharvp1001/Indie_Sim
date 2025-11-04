using UnityEngine;
using UnityEngine.UI;
using TMPro;
/*
public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI upgradeNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public Image upgradeIcon;
    public Button purchaseButton;

    [Header("Upgrade Type")]
    public UpgradeType upgradeType;

    [Header("Upgrade Values")]
    public float upgradeAmount = 5f;
    public int upgradeCost = 0;
    public string targetWeaponName = "HealthBoost";

    [Header("Base Values")]
    public int baseHealthValue = 100;
    public float baseSpeedValue = 5f;

    [Header("Colors")]
    public Color availableColor = Color.green;
    public Color alreadyUsedColor = Color.red;

    public enum UpgradeType
    {
        PlayerHealth,
        PlayerSpeed,
        WeaponDamage
    }

    private bool hasBeenUsed = false;

    void Start()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }

        SetupCard();
    }

    void Update()
    {
        UpdateDescription();
    }

    void OnDestroy()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
        }
    }

    void SetupCard()
    {
        if (upgradeIcon != null)
        {
            switch (upgradeType)
            {
                case UpgradeType.PlayerHealth:
                    upgradeIcon.color = Color.red;
                    break;
                case UpgradeType.PlayerSpeed:
                    upgradeIcon.color = Color.yellow;
                    break;
                case UpgradeType.WeaponDamage:
                    upgradeIcon.color = Color.cyan;
                    break;
            }
        }

        if (upgradeNameText != null)
        {
            upgradeNameText.text = GetUpgradeName();
        }

        if (costText != null)
        {
            costText.text = "FREE";
            costText.color = Color.green;
        }
    }

    string GetUpgradeName()
    {
        switch (upgradeType)
        {
            case UpgradeType.PlayerHealth:
                return "Health Boost";
            case UpgradeType.PlayerSpeed:
                return "Speed Boost";
            case UpgradeType.WeaponDamage:
                return $"{targetWeaponName} Damage";
            default:
                return "Unknown";
        }
    }

    void UpdateDescription()
    {
        if (descriptionText == null || UpgradeManager.Instance == null) return;

        float currentValue = 0f;
        float totalValue = 0f;
        string valueUnit = "";

        switch (upgradeType)
        {
            case UpgradeType.PlayerHealth:
                int healthBonus = UpgradeManager.Instance.healthBonus;
                currentValue = baseHealthValue + healthBonus;
                totalValue = currentValue + upgradeAmount;
                valueUnit = " HP";
                break;

            case UpgradeType.PlayerSpeed:
                float speedBonus = UpgradeManager.Instance.speedBonus;
                currentValue = baseSpeedValue + speedBonus;
                totalValue = currentValue + upgradeAmount;
                valueUnit = " speed";
                break;

            case UpgradeType.WeaponDamage:
                int weaponBonus = UpgradeManager.Instance.GetWeaponDamageBonus(targetWeaponName);
                currentValue = weaponBonus;
                totalValue = currentValue + upgradeAmount;
                valueUnit = " DMG";
                break;
        }

        string formattedText = $"<b>{currentValue:F0}{valueUnit}</b> + {upgradeAmount:F0} = <color=lime><b>{totalValue:F0}{valueUnit}</b></color>";
        descriptionText.text = formattedText;
    }

    void OnPurchaseClicked()
    {
        if (hasBeenUsed)
        {
            Debug.Log($"<color=red>Already used!</color>");
            return;
        }

        ApplyUpgrade();
        DisableAllUpgradeButtons();

        Debug.Log($"<color=lime>✓ Upgrade applied: {GetUpgradeName()}</color>");
    }

    void ApplyUpgrade()
    {
        if (UpgradeManager.Instance == null)
        {
            Debug.LogError("UpgradeManager not found!");
            return;
        }

        switch (upgradeType)
        {
            case UpgradeType.PlayerHealth:
                UpgradeManager.Instance.UpgradeHealth();
                break;

            case UpgradeType.PlayerSpeed:
                UpgradeManager.Instance.UpgradeSpeed();
                break;

            case UpgradeType.WeaponDamage:
                UpgradeManager.Instance.UpgradeWeaponDamage(targetWeaponName);
                break;
        }

        hasBeenUsed = true;
    }

    void DisableAllUpgradeButtons()
    {
        // Find all upgrade cards and disable them
        UpgradeCardUI[] allCards = FindObjectsOfType<UpgradeCardUI>();

        foreach (UpgradeCardUI card in allCards)
        {
            if (card.purchaseButton != null)
            {
                card.purchaseButton.interactable = false;

                ColorBlock colors = card.purchaseButton.colors;
                colors.normalColor = alreadyUsedColor;
                card.purchaseButton.colors = colors;
            }

            card.hasBeenUsed = true;
        }

        Debug.Log("<color=red>✓ All upgrade buttons disabled (one per level)</color>");
    }
}
*/
