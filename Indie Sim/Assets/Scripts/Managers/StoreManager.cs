using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoreManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject storePanel;
    public GameObject upgradesPanel;
    public Button continueButton;
    public Button rerollButton;

    [Header("All Upgrade Buttons")]
    public List<Button> allUpgradeButtons;

    [Header("Weapon-Specific Upgrade Buttons")]
    public Button shotgunUpgradeButton;
    public Button machineGunUpgradeButton;

    [Header("Weapon Unlock Buttons")]
    public Button unlockShotgunButton;
    public Button unlockMachineGunButton;

    [Header("Player References")]
    [Tooltip("✅ NEW: Reference to WeaponInventory to get weapon list")]
    public WeaponInventory weaponInventory;

    [Header("Configuration")]
    public int buttonsToEnable = 2;

    private List<Button> unlockedUpgradeButtons = new List<Button>();
    private bool upgradeSelected = false;

    private void Start()
    {
        // ✅ Auto-find WeaponInventory if not assigned
        if (weaponInventory == null)
        {
            weaponInventory = FindObjectOfType<WeaponInventory>();
        }

        InitializeUnlockedUpgrades();

        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }

        Debug.Log($"[StoreManager] Initialized with {unlockedUpgradeButtons.Count} unlocked upgrades out of {allUpgradeButtons.Count} total");
    }

    private void InitializeUnlockedUpgrades()
    {
        unlockedUpgradeButtons.Clear();

        foreach (Button button in allUpgradeButtons)
        {
            if (button == null)
                continue;

            if (button == shotgunUpgradeButton || button == machineGunUpgradeButton)
            {
                Debug.Log($"[StoreManager] Skipping locked weapon upgrade: {button.name}");
                continue;
            }

            unlockedUpgradeButtons.Add(button);
            Debug.Log($"[StoreManager] Added to unlocked upgrades: {button.name}");
        }

        Debug.Log($"[StoreManager] Initial unlocked upgrades: {unlockedUpgradeButtons.Count}");
    }

    public void UnlockShotgunUpgrade()
    {
        Debug.Log("[StoreManager] Processing Shotgun unlock...");

        if (unlockShotgunButton != null && unlockedUpgradeButtons.Contains(unlockShotgunButton))
        {
            unlockedUpgradeButtons.Remove(unlockShotgunButton);
            Debug.Log($"[StoreManager] ❌ Removed 'Unlock Shotgun' button from pool");
        }

        if (shotgunUpgradeButton == null)
        {
            Debug.LogWarning("[StoreManager] Shotgun upgrade button is not assigned!");
            return;
        }

        if (unlockedUpgradeButtons.Contains(shotgunUpgradeButton))
        {
            Debug.Log("[StoreManager] Shotgun upgrade already in pool");
            return;
        }

        unlockedUpgradeButtons.Add(shotgunUpgradeButton);
        Debug.Log($"[StoreManager] ✅ Shotgun upgrade unlocked! Now {unlockedUpgradeButtons.Count} upgrades available");
    }

    public void UnlockMachineGunUpgrade()
    {
        Debug.Log("[StoreManager] Processing Machine Gun unlock...");

        if (unlockMachineGunButton != null && unlockedUpgradeButtons.Contains(unlockMachineGunButton))
        {
            unlockedUpgradeButtons.Remove(unlockMachineGunButton);
            Debug.Log($"[StoreManager] ❌ Removed 'Unlock Machine Gun' button from pool");
        }

        if (machineGunUpgradeButton == null)
        {
            Debug.LogWarning("[StoreManager] Machine Gun upgrade button is not assigned!");
            return;
        }

        if (unlockedUpgradeButtons.Contains(machineGunUpgradeButton))
        {
            Debug.Log("[StoreManager] Machine Gun upgrade already in pool");
            return;
        }

        unlockedUpgradeButtons.Add(machineGunUpgradeButton);
        Debug.Log($"[StoreManager] ✅ Machine Gun upgrade unlocked! Now {unlockedUpgradeButtons.Count} upgrades available");
    }

    /// <summary>
    /// ✅ FIXED: Now uses WeaponUnlockManager to check unlock status
    /// </summary>
    private void UpdateWeaponUpgradeAvailability()
    {
        if (weaponInventory == null || WeaponUnlockManager.Instance == null)
            return;

        // Check Shotgun
        int shotgunIndex = FindWeaponIndexByType(WeaponData.WeaponType.Shotgun);
        if (shotgunIndex != -1)
        {
            WeaponData shotgun = weaponInventory.GetAllWeapons()[shotgunIndex];
            if (WeaponUnlockManager.Instance.IsWeaponUnlocked(shotgun))
            {
                UnlockShotgunUpgrade();
            }
        }

        // Check Machine Gun
        int mgIndex = FindWeaponIndexByName("Machine", "AK");
        if (mgIndex != -1)
        {
            WeaponData machineGun = weaponInventory.GetAllWeapons()[mgIndex];
            if (WeaponUnlockManager.Instance.IsWeaponUnlocked(machineGun))
            {
                UnlockMachineGunUpgrade();
            }
        }
    }

    public void OpenStore()
    {
        EnableAllUpgradeButtons();
        rerollButton.gameObject.SetActive(true);

        UpdateWeaponUpgradeAvailability();

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

        upgradeSelected = false;

        if (storePanel != null)
        {
            storePanel.SetActive(true);
        }

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }

        RandomizeStoreButtons();

        Debug.Log($"Store opened with {unlockedUpgradeButtons.Count} available upgrades");
    }

    private void RandomizeStoreButtons()
    {
        foreach (Button button in allUpgradeButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }

        List<Button> availableButtons = new List<Button>(unlockedUpgradeButtons);

        for (int i = 0; i < buttonsToEnable; i++)
        {
            if (availableButtons.Count == 0)
                break;

            int randomIndex = Random.Range(0, availableButtons.Count);
            Button selectedButton = availableButtons[randomIndex];

            if (selectedButton != null)
            {
                selectedButton.gameObject.SetActive(true);
                Debug.Log($"Enabled button: {selectedButton.name}");
            }

            availableButtons.RemoveAt(randomIndex);
        }

        Debug.Log($"Store randomized: {buttonsToEnable} buttons enabled out of {unlockedUpgradeButtons.Count} unlocked upgrades");
    }

    public void RerollUpgrades()
    {
        if (unlockedUpgradeButtons == null || unlockedUpgradeButtons.Count == 0)
        {
            Debug.LogWarning("No unlocked upgrade buttons available for re-roll!");
            return;
        }

        upgradeSelected = false;

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }

        EnableAllUpgradeButtons();
        RandomizeStoreButtons();
        RefreshAllPreviewDisplays();

        Debug.Log("Upgrades re-rolled!");
    }

    public void OnUpgradeSelected()
    {
        if (upgradeSelected)
        {
            Debug.Log("Upgrade already selected");
            return;
        }

        upgradeSelected = true;

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            rerollButton.gameObject.SetActive(false);
            Debug.Log("Continue button shown - player can now close the store");
        }

        DisableAllUpgradeButtons();
    }

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

    public void CloseStore()
    {
        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }

        Debug.Log("Store closed - dungeon continues");
    }

    // ===== HELPER METHODS =====

    /// <summary>
    /// ✅ FIXED: Now uses WeaponInventory instead of PlayerConeShooter
    /// </summary>
    private int FindWeaponIndexByType(WeaponData.WeaponType weaponType)
    {
        if (weaponInventory == null)
            return -1;

        WeaponData[] weapons = weaponInventory.GetAllWeapons();
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].weaponType == weaponType)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// ✅ FIXED: Now uses WeaponInventory instead of PlayerConeShooter
    /// </summary>
    private int FindWeaponIndexByName(params string[] names)
    {
        if (weaponInventory == null)
            return -1;

        WeaponData[] weapons = weaponInventory.GetAllWeapons();
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
