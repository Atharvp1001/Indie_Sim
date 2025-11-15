using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoreManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The main store panel (parent of UpgradesPanel)")]
    public GameObject storePanel;

    [Tooltip("Panel containing all upgrade buttons with Grid Layout Group")]
    public GameObject upgradesPanel;

    [Tooltip("Button that appears after player selects an upgrade")]
    public Button continueButton;

    [Tooltip("Button to reroll upgrades")]
    public Button rerollButton;

    [Header("All Upgrade Buttons")]
    [Tooltip("Assign ALL upgrade buttons here (including locked weapon upgrades)")]
    public List<Button> allUpgradeButtons;

    [Header("Weapon-Specific Upgrade Buttons")]
    [Tooltip("The button that upgrades Shotgun damage")]
    public Button shotgunUpgradeButton;

    [Tooltip("The button that upgrades Machine Gun damage")]
    public Button machineGunUpgradeButton;

    [Header("Weapon Unlock Buttons")]
    [Tooltip("The button that UNLOCKS the Shotgun (will be removed when shotgun is unlocked)")]
    public Button unlockShotgunButton;

    [Tooltip("The button that UNLOCKS the Machine Gun (will be removed when MG is unlocked)")]
    public Button unlockMachineGunButton;

    [Header("Player Reference")]
    [Tooltip("Reference to PlayerConeShooter to check weapon unlock status")]
    public PlayerConeShooter playerShooter;

    [Header("Configuration")]
    [Tooltip("Number of buttons to enable randomly (default: 2)")]
    public int buttonsToEnable = 2;

    // This list contains only the upgrades the player can currently access
    private List<Button> unlockedUpgradeButtons = new List<Button>();

    private bool upgradeSelected = false;

    private void Start()
    {
        // Auto-find PlayerConeShooter if not assigned
        if (playerShooter == null)
        {
            playerShooter = FindObjectOfType<PlayerConeShooter>();
        }

        // Initialize the unlocked upgrades list
        InitializeUnlockedUpgrades();

        // Make sure the store is hidden at start
        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }

        // Hide continue button at start
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }

        Debug.Log($"[StoreManager] Initialized with {unlockedUpgradeButtons.Count} unlocked upgrades out of {allUpgradeButtons.Count} total");
    }

    /// <summary>
    /// Populates unlockedUpgradeButtons with all buttons EXCEPT shotgun/machinegun upgrades
    /// Shotgun/MachineGun upgrades are only added when the weapons are unlocked
    /// </summary>
    private void InitializeUnlockedUpgrades()
    {
        unlockedUpgradeButtons.Clear();

        foreach (Button button in allUpgradeButtons)
        {
            if (button == null)
                continue;

            // Skip shotgun and machine gun upgrade buttons - they'll be added when unlocked
            if (button == shotgunUpgradeButton || button == machineGunUpgradeButton)
            {
                Debug.Log($"[StoreManager] Skipping locked weapon upgrade: {button.name}");
                continue;
            }

            // Add all other buttons to unlocked list
            unlockedUpgradeButtons.Add(button);
            Debug.Log($"[StoreManager] Added to unlocked upgrades: {button.name}");
        }

        Debug.Log($"[StoreManager] Initial unlocked upgrades: {unlockedUpgradeButtons.Count}");
    }

    /// <summary>
    /// Call this when Shotgun is unlocked to add its upgrade button to the pool
    /// Can be called from UpgradeManager.UnlockShotgun() or wherever shotgun unlocks
    /// </summary>
    public void UnlockShotgunUpgrade()
    {
        Debug.Log("[StoreManager] Processing Shotgun unlock...");

        // ✅ NEW: Remove the unlock button from the pool
        if (unlockShotgunButton != null && unlockedUpgradeButtons.Contains(unlockShotgunButton))
        {
            unlockedUpgradeButtons.Remove(unlockShotgunButton);
            Debug.Log($"[StoreManager] ❌ Removed 'Unlock Shotgun' button from pool");
        }

        // Add the upgrade button
        if (shotgunUpgradeButton == null)
        {
            Debug.LogWarning("[StoreManager] Shotgun upgrade button is not assigned!");
            return;
        }

        // Check if already unlocked
        if (unlockedUpgradeButtons.Contains(shotgunUpgradeButton))
        {
            Debug.Log("[StoreManager] Shotgun upgrade already in pool");
            return;
        }

        // Add to unlocked list
        unlockedUpgradeButtons.Add(shotgunUpgradeButton);
        Debug.Log($"[StoreManager] ✅ Shotgun upgrade unlocked! Now {unlockedUpgradeButtons.Count} upgrades available");
    }

    /// <summary>
    /// Call this when Machine Gun is unlocked to add its upgrade button to the pool
    /// Can be called from UpgradeManager.UnlockMachineGun() or wherever MG unlocks
    /// </summary>
    public void UnlockMachineGunUpgrade()
    {
        Debug.Log("[StoreManager] Processing Machine Gun unlock...");

        // ✅ NEW: Remove the unlock button from the pool
        if (unlockMachineGunButton != null && unlockedUpgradeButtons.Contains(unlockMachineGunButton))
        {
            unlockedUpgradeButtons.Remove(unlockMachineGunButton);
            Debug.Log($"[StoreManager] ❌ Removed 'Unlock Machine Gun' button from pool");
        }

        // Add the upgrade button
        if (machineGunUpgradeButton == null)
        {
            Debug.LogWarning("[StoreManager] Machine Gun upgrade button is not assigned!");
            return;
        }

        // Check if already unlocked
        if (unlockedUpgradeButtons.Contains(machineGunUpgradeButton))
        {
            Debug.Log("[StoreManager] Machine Gun upgrade already in pool");
            return;
        }

        // Add to unlocked list
        unlockedUpgradeButtons.Add(machineGunUpgradeButton);
        Debug.Log($"[StoreManager] ✅ Machine Gun upgrade unlocked! Now {unlockedUpgradeButtons.Count} upgrades available");
    }

    /// <summary>
    /// Automatically checks weapon unlock status and adds their upgrade buttons if unlocked
    /// Call this in OpenStore() to keep it synchronized
    /// </summary>
    private void UpdateWeaponUpgradeAvailability()
    {
        if (playerShooter == null)
            return;

        // Check Shotgun
        int shotgunIndex = FindWeaponIndexByType(WeaponData.WeaponType.Shotgun);
        if (shotgunIndex != -1 && playerShooter.IsWeaponUnlocked(shotgunIndex))
        {
            UnlockShotgunUpgrade();
        }

        // Check Machine Gun (search by name since it might not have a specific enum)
        int mgIndex = FindWeaponIndexByName("Machine", "AK");
        if (mgIndex != -1 && playerShooter.IsWeaponUnlocked(mgIndex))
        {
            UnlockMachineGunUpgrade();
        }
    }

    /// <summary>
    /// Opens the store and randomizes which upgrades appear
    /// Called by RoguelikeManager after dungeon completion
    /// </summary>
    public void OpenStore()
    {
        EnableAllUpgradeButtons();
        rerollButton.gameObject.SetActive(true);
        // Update weapon upgrade availability based on current unlock status
        UpdateWeaponUpgradeAvailability();

        // Validate setup
        if (unlockedUpgradeButtons == null || unlockedUpgradeButtons.Count == 0)
        {
            Debug.LogWarning("No unlocked upgrade buttons available!");
            return;
        }

        if (buttonsToEnable > unlockedUpgradeButtons.Count)
        {
            Debug.LogWarning($"Buttons to enable ({buttonsToEnable}) is greater than unlocked buttons ({unlockedUpgradeButtons.Count}). Adjusting to maximum.");
            buttonsToEnable = unlockedUpgradeButtons.Count;
        }

        // Reset state
        upgradeSelected = false;

        // Show the store panel
        if (storePanel != null)
        {
            storePanel.SetActive(true);
        }

        // Hide continue button
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }

        // Randomize which upgrade buttons appear (only from unlocked pool)
        RandomizeStoreButtons();

        Debug.Log($"Store opened with {unlockedUpgradeButtons.Count} available upgrades");
    }

    /// <summary>
    /// Disables all buttons, then randomly enables the specified number
    /// NOW USES unlockedUpgradeButtons instead of allUpgradeButtons
    /// </summary>
    private void RandomizeStoreButtons()
    {
        // First, disable all buttons
        foreach (Button button in allUpgradeButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }

        // Create a copy of the UNLOCKED buttons list to pick from
        List<Button> availableButtons = new List<Button>(unlockedUpgradeButtons);

        // Randomly enable the specified number of buttons
        for (int i = 0; i < buttonsToEnable; i++)
        {
            if (availableButtons.Count == 0)
                break;

            // Pick a random button from the available list
            int randomIndex = Random.Range(0, availableButtons.Count);
            Button selectedButton = availableButtons[randomIndex];

            // Enable the button
            if (selectedButton != null)
            {
                selectedButton.gameObject.SetActive(true);
                Debug.Log($"Enabled button: {selectedButton.name}");
            }

            // Remove from available list so it can't be picked again
            availableButtons.RemoveAt(randomIndex);
        }

        Debug.Log($"Store randomized: {buttonsToEnable} buttons enabled out of {unlockedUpgradeButtons.Count} unlocked upgrades");
    }

    /// <summary>
    /// Re-rolls the available upgrades
    /// Disables all buttons and randomly enables the specified number again
    /// Call this from a "Re-roll" button's OnClick event
    /// </summary>
    public void RerollUpgrades()
    {
        // Validate setup
        if (unlockedUpgradeButtons == null || unlockedUpgradeButtons.Count == 0)
        {
            Debug.LogWarning("No unlocked upgrade buttons available for re-roll!");
            return;
        }

        // Reset the upgrade selection state so player can pick again
        upgradeSelected = false;

        // Hide continue button since we're re-rolling
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }

        // Re-enable all buttons in case they were disabled after a previous selection
        EnableAllUpgradeButtons();

        // Randomize which buttons appear (uses unlocked buttons only)
        RandomizeStoreButtons();

        // Optional: Refresh preview displays if you added the preview system
        RefreshAllPreviewDisplays();

        Debug.Log("Upgrades re-rolled!");
    }

    /// <summary>
    /// Call this when an upgrade button is clicked
    /// This will show the continue button
    /// </summary>
    public void OnUpgradeSelected()
    {
        if (upgradeSelected)
        {
            Debug.Log("Upgrade already selected");
            return;
        }

        upgradeSelected = true;

        // Show the continue button
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            rerollButton.gameObject.SetActive(false); // Hide reroll button after selection
            Debug.Log("Continue button shown - player can now close the store");
        }

        // Optional: Disable all upgrade buttons so player can't select multiple
        DisableAllUpgradeButtons();

    }

    /// <summary>
    /// Disables all upgrade buttons after one is selected
    /// </summary>
    private void DisableAllUpgradeButtons()
    {
        foreach (Button button in allUpgradeButtons)
        {
            if (button != null)
            {
                button.interactable = false;
            }
        }
    }

    /// <summary>
    /// Re-enables all upgrade buttons
    /// </summary>
    private void EnableAllUpgradeButtons()
    {
        foreach (Button button in allUpgradeButtons)
        {
            if (button != null)
            {
                button.interactable = true;
            }
        }
    }

    /// <summary>
    /// Refreshes all preview displays on active buttons
    /// Only needed if you're using the UpgradePreviewDisplay script
    /// </summary>
    private void RefreshAllPreviewDisplays()
    {
        foreach (Button button in allUpgradeButtons)
        {
            if (button != null && button.gameObject.activeSelf)
            {
                UpgradePreviewDisplay preview = button.GetComponent<UpgradePreviewDisplay>();
                if (preview != null)
                {
                    preview.UpdatePreview();
                }
            }
        }
    }

    /// <summary>
    /// Closes the store panel
    /// Called by the Continue button
    /// </summary>
    public void CloseStore()
    {
        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }

        Debug.Log("Store closed - dungeon continues");
    }

    // ===== HELPER METHODS (same as UpgradeManager) =====

    private int FindWeaponIndexByType(WeaponData.WeaponType weaponType)
    {
        if (playerShooter == null)
            return -1;

        WeaponData[] weapons = playerShooter.GetAllWeapons();
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].weaponType == weaponType)
                return i;
        }
        return -1;
    }

    private int FindWeaponIndexByName(params string[] names)
    {
        if (playerShooter == null)
            return -1;

        WeaponData[] weapons = playerShooter.GetAllWeapons();
        for (int i = 0; i < weapons.Length; i++)
        {
            foreach (string name in names)
            {
                if (weapons[i].weaponName.ToLower().Contains(name.ToLower()))
                    return i;
            }
        }
        return -1;
    }

    public bool IsUpgradeSelected()
    {
        return upgradeSelected;
    }
}
