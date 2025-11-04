using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UpgradeManager : MonoBehaviour
{
    /*
    // Singleton instance
    public static UpgradeManager Instance { get; private set; }

    [Header("Player Upgrade Increments")]
    public float speedUpgradeAmount = 1f;
    public int healthUpgradeAmount = 25;

    [Header("Weapon Upgrade Increments")]
    public int weaponDamageUpgradeAmount = 5;

    [Header("Player Test Buttons")]
    public Button speedUpgradeButton;
    public Button healthUpgradeButton;

    [Header("Weapon Test Buttons")]
    public Button pistolDamageUpgradeButton;
    public Button MachineGunDamageUpgradeButton;
    public Button shotgunDamageUpgradeButton;

    [Header("Debug")]
    public bool debugMode = true;

    // Player bonus values
    public float speedBonus { get; private set; } = 0f;
    public int healthBonus { get; private set; } = 0;

    // Weapon damage bonuses - tracked per weapon by name
    private Dictionary<string, int> weaponDamageBonuses = new Dictionary<string, int>();

    // Track upgrade counts
    private int speedUpgradeCount = 0;
    private int healthUpgradeCount = 0;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Load upgrades from GameSessionData
        LoadUpgradesFromSession();

        // Add player button listeners
        if (speedUpgradeButton != null)
        {
            speedUpgradeButton.onClick.AddListener(UpgradeSpeed);
        }

        if (healthUpgradeButton != null)
        {
            healthUpgradeButton.onClick.AddListener(UpgradeHealth);
        }

        // Add weapon button listeners
        if (pistolDamageUpgradeButton != null)
        {
            pistolDamageUpgradeButton.onClick.AddListener(() => UpgradeWeaponDamage("Pistol"));
        }

        if (MachineGunDamageUpgradeButton != null)
        {
            MachineGunDamageUpgradeButton.onClick.AddListener(() => UpgradeWeaponDamage("MachineGun"));
        }

        if (shotgunDamageUpgradeButton != null)
        {
            shotgunDamageUpgradeButton.onClick.AddListener(() => UpgradeWeaponDamage("Shotgun"));
        }
    }

    void OnDestroy()
    {
        // Clean up button listeners
        if (speedUpgradeButton != null)
        {
            speedUpgradeButton.onClick.RemoveListener(UpgradeSpeed);
        }

        if (healthUpgradeButton != null)
        {
            healthUpgradeButton.onClick.RemoveListener(UpgradeHealth);
        }

        if (pistolDamageUpgradeButton != null)
        {
            pistolDamageUpgradeButton.onClick.RemoveAllListeners();
        }

        if (MachineGunDamageUpgradeButton != null)
        {
            MachineGunDamageUpgradeButton.onClick.RemoveAllListeners();
        }

        if (shotgunDamageUpgradeButton != null)
        {
            shotgunDamageUpgradeButton.onClick.RemoveAllListeners();
        }
    }

    void OnApplicationQuit()
    {
        // Save when game closes
        SaveUpgradesToSession();
    }

    // ========== SAVE/LOAD FROM GAME SESSION DATA ==========

    /// <summary>
    /// Load upgrades from GameSessionData
    /// </summary>
    void LoadUpgradesFromSession()
    {
        if (PersistentDataManager.Instance == null ||
            PersistentDataManager.Instance.currentSession == null)
        {
            Debug.LogWarning("PersistentDataManager not found - cannot load upgrades");
            return;
        }

        var session = PersistentDataManager.Instance.currentSession;

        // Load player upgrades
        speedUpgradeCount = session.speedUpgradeLevel;
        healthUpgradeCount = session.healthUpgradeLevel;

        speedBonus = speedUpgradeCount * speedUpgradeAmount;
        healthBonus = healthUpgradeCount * healthUpgradeAmount;

        // Load weapon damage bonuses
        weaponDamageBonuses["Pistol"] = session.pistolDamageBonus;
        weaponDamageBonuses["MachineGun"] = session.MachineGunDamageBonus;
        weaponDamageBonuses["Shotgun"] = session.shotgunDamageBonus;

        if (debugMode)
        {
            Debug.Log("<color=cyan>✓ LOADED UPGRADES FROM SESSION DATA</color>");
            Debug.Log($"Speed Level: {speedUpgradeCount}, Bonus: +{speedBonus}");
            Debug.Log($"Health Level: {healthUpgradeCount}, Bonus: +{healthBonus}");
            Debug.Log($"Pistol: +{weaponDamageBonuses["Pistol"]}, AK: +{weaponDamageBonuses["MachineGun"]}, Shotgun: +{weaponDamageBonuses["Shotgun"]}");
        }
    }

    /// <summary>
    /// Save upgrades to GameSessionData
    /// </summary>
    void SaveUpgradesToSession()
    {
        if (PersistentDataManager.Instance == null ||
            PersistentDataManager.Instance.currentSession == null)
        {
            Debug.LogWarning("PersistentDataManager not found - cannot save upgrades");
            return;
        }

        var session = PersistentDataManager.Instance.currentSession;

        // Save player upgrades
        session.speedUpgradeLevel = speedUpgradeCount;
        session.healthUpgradeLevel = healthUpgradeCount;

        // Save weapon damage bonuses
        session.pistolDamageBonus = GetWeaponDamageBonus("Pistol");
        session.MachineGunDamageBonus = GetWeaponDamageBonus("MachineGun");
        session.shotgunDamageBonus = GetWeaponDamageBonus("Shotgun");

        // Save to PlayerPrefs (persists between sessions)
        string json = JsonUtility.ToJson(session);
        PlayerPrefs.SetString("GameSessionData", json);
        PlayerPrefs.Save();

        if (debugMode)
        {
            Debug.Log("<color=green>✓ SAVED UPGRADES TO SESSION DATA</color>");
            Debug.Log($"Speed: {speedUpgradeCount}, Health: {healthUpgradeCount}");
            Debug.Log($"Pistol: {session.pistolDamageBonus}, MachineGun: {session.MachineGunDamageBonus}, Shotgun: {session.shotgunDamageBonus}");
        }
    }

    // ========== PLAYER UPGRADES ==========

    public void UpgradeSpeed()
    {
        speedUpgradeCount++;
        speedBonus = speedUpgradeCount * speedUpgradeAmount;

        // Update session data
        if (PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.currentSession.speedUpgradeLevel = speedUpgradeCount;
        }

        Debug.Log($"<color=cyan>Speed Upgraded! Level: {speedUpgradeCount}, Speed Bonus: +{speedBonus}</color>");
    }

    public void UpgradeHealth()
    {
        healthUpgradeCount++;
        int oldBonus = healthBonus;
        healthBonus = healthUpgradeCount * healthUpgradeAmount;
        int healthAdded = healthBonus - oldBonus;

        // Update session data
        if (PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.currentSession.healthUpgradeLevel = healthUpgradeCount;
            PersistentDataManager.Instance.currentSession.maxHealthBonus = healthBonus;
        }

        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.AddHealth(healthAdded);
        }

        Debug.Log($"<color=green>Health Upgraded! Level: {healthUpgradeCount}, Health Bonus: +{healthBonus}</color>");
    }

    // ========== WEAPON UPGRADES ==========

    /// <summary>
    /// Upgrade a specific weapon's damage
    /// </summary>
    public void UpgradeWeaponDamage(string weaponName)
    {
        // Initialize if not exists
        if (!weaponDamageBonuses.ContainsKey(weaponName))
        {
            weaponDamageBonuses[weaponName] = 0;
        }

        // Increase damage bonus
        weaponDamageBonuses[weaponName] += weaponDamageUpgradeAmount;

        int currentBonus = weaponDamageBonuses[weaponName];
        int upgradeLevel = currentBonus / weaponDamageUpgradeAmount;

        // Update session data
        if (PersistentDataManager.Instance != null)
        {
            var session = PersistentDataManager.Instance.currentSession;

            if (weaponName == "Pistol")
                session.pistolDamageBonus = currentBonus;
            else if (weaponName == "MachineGun")
                session.MachineGunDamageBonus = currentBonus;
            else if (weaponName == "Shotgun")
                session.shotgunDamageBonus = currentBonus;
        }

        Debug.Log($"<color=yellow>{weaponName} Damage Upgraded! Level: {upgradeLevel}, Damage Bonus: +{currentBonus}</color>");
    }

    /// <summary>
    /// Get the damage bonus for a specific weapon
    /// </summary>
    public int GetWeaponDamageBonus(string weaponName)
    {
        if (weaponDamageBonuses.ContainsKey(weaponName))
        {
            return weaponDamageBonuses[weaponName];
        }
        return 0;
    }

    /// <summary>
    /// Get the upgrade level for a specific weapon's damage
    /// </summary>
    public int GetWeaponDamageUpgradeLevel(string weaponName)
    {
        int bonus = GetWeaponDamageBonus(weaponName);
        return bonus / weaponDamageUpgradeAmount;
    }

    // ========== DEBUG METHODS ==========

    [ContextMenu("Print Upgrade Status")]
    public void PrintUpgradeStatus()
    {
        Debug.Log("<color=yellow>========== UPGRADE STATUS ==========</color>");
        Debug.Log($"Speed: Level {speedUpgradeCount}, Bonus +{speedBonus}");
        Debug.Log($"Health: Level {healthUpgradeCount}, Bonus +{healthBonus}");
        Debug.Log($"Pistol Damage: +{GetWeaponDamageBonus("Pistol")}");
        Debug.Log($"MachineGun Damage: +{GetWeaponDamageBonus("MachineGun")}");
        Debug.Log($"Shotgun Damage: +{GetWeaponDamageBonus("Shotgun")}");
        Debug.Log("<color=yellow>===================================</color>");
    }

    [ContextMenu("Reset All Upgrades")]
    public void ResetAllUpgrades()
    {
        speedUpgradeCount = 0;
        healthUpgradeCount = 0;
        speedBonus = 0f;
        healthBonus = 0;

        weaponDamageBonuses.Clear();
        weaponDamageBonuses["Pistol"] = 0;
        weaponDamageBonuses["MachineGun"] = 0;
        weaponDamageBonuses["Shotgun"] = 0;

        SaveUpgradesToSession();

        Debug.Log("<color=red>✓ ALL UPGRADES RESET</color>");
    }
    */
}
