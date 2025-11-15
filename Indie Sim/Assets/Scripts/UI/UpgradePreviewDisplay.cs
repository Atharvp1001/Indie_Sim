using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Displays upgrade preview text: "Current + Bonus = New"
/// Attach this to each upgrade button
/// </summary>
public class UpgradePreviewDisplay : MonoBehaviour
{
    public enum UpgradeType
    {
        Speed,
        Health,
        PistolDamage,
        ShotgunDamage,
        MachineGunDamage,
        UnlockShotgun,      // ✅ NEW: For the unlock shotgun button
        UnlockMachineGun    // ✅ NEW: For the unlock machine gun button
    }


    [Header("What does this button upgrade?")]
    [SerializeField] private UpgradeType upgradeType;

    [Header("References")]
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private TextMeshProUGUI previewText;

    [Header("Optional - For Health Preview")]
    [SerializeField] private PlayerHealth playerHealth;

    void Start()
    {
        // Auto-find UpgradeManager if not assigned
        if (upgradeManager == null)
            upgradeManager = FindObjectOfType<UpgradeManager>();

        // Auto-find PlayerHealth if needed and not assigned
        if (playerHealth == null && upgradeType == UpgradeType.Health)
            playerHealth = FindObjectOfType<PlayerHealth>();
    }

    void OnEnable()
    {
        // ✅ FIX: Update preview whenever button is enabled (when store opens/rerolls)
        // Use coroutine to wait one frame for all references to initialize
        StartCoroutine(UpdatePreviewDelayed());
    }

  

    /// <summary>
    /// Call this to refresh the preview text
    /// </summary>
    public void UpdatePreview()
    {
        if (upgradeManager == null)
        {
            Debug.LogWarning($"[UpgradePreview] UpgradeManager not found for {gameObject.name}!");
            return;
        }

        if (previewText == null)
        {
            Debug.LogWarning($"[UpgradePreview] PreviewText not assigned for {gameObject.name}!");
            return;
        }

        string previewString = "";

        switch (upgradeType)
        {
            case UpgradeType.Speed:
                float currentSpeed = upgradeManager.GetCurrentPlayerSpeed();
                float speedBonus = upgradeManager.GetSpeedUpgradeBonus();
                float newSpeed = currentSpeed + speedBonus;
                previewString = $"Upgrade Speed: {currentSpeed:0.#} → {newSpeed:0.#}";
                break;

            case UpgradeType.Health:
                int currentHP = 0;
                if (playerHealth != null)
                    currentHP = playerHealth.currentHealth;
                else
                    Debug.LogWarning($"[UpgradePreview] PlayerHealth not assigned for Health upgrade on {gameObject.name}!");

                int healthBonus = 25;
                int newHP = currentHP + healthBonus;
                previewString = $"Heal: +{healthBonus} HP";
                break;

            case UpgradeType.PistolDamage:
                int currentPistol = upgradeManager.GetCurrentPistolDamage();
                int damageBonus = upgradeManager.GetDamageUpgradeBonus();
                int newPistol = currentPistol + damageBonus;
                previewString = $"Upgrade Pistol: {currentPistol} → {newPistol} DMG";
                break;

            case UpgradeType.ShotgunDamage:
                int currentShotgun = upgradeManager.GetCurrentShotgunDamage();
                int shotgunBonus = upgradeManager.GetDamageUpgradeBonus();
                int newShotgun = currentShotgun + shotgunBonus;
                previewString = $"Upgrade Shotgun: {currentShotgun} → {newShotgun} DMG";
                break;

            case UpgradeType.MachineGunDamage:
                int currentMG = upgradeManager.GetCurrentMachineGunDamage();
                int mgBonus = upgradeManager.GetDamageUpgradeBonus();
                int newMG = currentMG + mgBonus;
                previewString = $"Upgrade MG: {currentMG} → {newMG} DMG";
                break;

            case UpgradeType.UnlockShotgun:
                // Show unlock text for shotgun
                previewString = " Unlock Shotgun";
                break;

            case UpgradeType.UnlockMachineGun:
                // Show unlock text for machine gun
                previewString = " Unlock Machine Gun";
                break;
        }

        // Update the text
        previewText.text = previewString;

        Debug.Log($"[UpgradePreview] Updated {gameObject.name}: {previewString}");
    }

    /// <summary>
    /// Call this after purchasing to refresh the display
    /// Uses coroutine to wait for upgrade to fully apply
    /// </summary>
    public void OnUpgradePurchased()
    {
        // ✅ FIX: Wait longer and use coroutine for better timing
        StartCoroutine(UpdatePreviewAfterPurchase());
    }

    /// <summary>
    /// Waits one frame before updating to ensure upgrade is applied
    /// </summary>
    private IEnumerator UpdatePreviewDelayed()
    {
        yield return null; // Wait one frame
        UpdatePreview();
    }

    /// <summary>
    /// Waits for upgrade to be applied before refreshing preview
    /// </summary>
    private IEnumerator UpdatePreviewAfterPurchase()
    {
        // Wait for the OnClick events from Inspector to fire and apply the upgrade
        yield return new WaitForSeconds(0.2f);
        UpdatePreview();
    }
}
