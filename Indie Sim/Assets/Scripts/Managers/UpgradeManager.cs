using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerController playerMovement; // For speed upgrades
    [SerializeField] private PlayerHealth playerHealth;     // For health upgrades
    [SerializeField] private PlayerConeShooter playerShooter; // For gun damage upgrades

    [Header("Speed Upgrade")]
    [SerializeField] private float basePlayerSpeed = 5f;
    [SerializeField] private float speedUpgradeBonus = 1f; // How much speed to add per upgrade
    private int speedUpgradeLevel = 0;

    [Header("Health Upgrade")]
    [SerializeField] private int baseMaxHealth = 100;
    [SerializeField] private int healthUpgradeBonus = 20; // How much health to add per upgrade
    private int healthUpgradeLevel = 0;

    [Header("Gun Damage Upgrade")]
    [SerializeField] private int basePistolDamage = 10;
    [SerializeField] private int baseShotgunDamage = 25;
    [SerializeField] private int baseMachineGunDamage = 8;
    [SerializeField] private int damageUpgradeBonus = 2; // How much damage to add per upgrade
    private int pistolDamageLevel = 0;
    private int shotgunDamageLevel = 0;
    private int machineGunDamageLevel = 0;

    private void Start()
    {
        ValidateReferences();
        Debug.Log("[UpgradeManager] Initialized successfully");
        PrintUpgradeStats();
    }

    private void ValidateReferences()
    {
        if (playerMovement == null)
            Debug.LogError("[UpgradeManager] PlayerMovement reference not assigned!");
        if (playerHealth == null)
            Debug.LogError("[UpgradeManager] PlayerHealth reference not assigned!");
        if (playerShooter == null)
            Debug.LogError("[UpgradeManager] PlayerConeShooter reference not assigned!");
    }

    // ===== SPEED UPGRADES =====

    /// <summary>
    /// Upgrade player movement speed
    /// Formula: basePlayerSpeed + (speedUpgradeLevel * speedUpgradeBonus)
    /// </summary>
    public void UpgradePlayerSpeed()
    {
        if (playerMovement == null)
        {
            Debug.LogError("[UpgradeManager] Cannot upgrade speed - PlayerMovement not assigned!");
            return;
        }

        speedUpgradeLevel++;
        float newSpeed = basePlayerSpeed + (speedUpgradeLevel * speedUpgradeBonus);
        playerMovement.SetSpeed(newSpeed);

        Debug.Log($"[UpgradeManager] ✅ SPEED UPGRADED! Level {speedUpgradeLevel}");
        Debug.Log($"[UpgradeManager] New Speed: {newSpeed} (Base: {basePlayerSpeed} + Upgrades: {speedUpgradeLevel * speedUpgradeBonus})");
    }

    public int GetSpeedUpgradeLevel() => speedUpgradeLevel;
    public float GetCurrentPlayerSpeed() => basePlayerSpeed + (speedUpgradeLevel * speedUpgradeBonus);

    // ===== HEALTH UPGRADES =====

    /// <summary>
    /// Upgrade player health by adding +25 HP to CURRENT health (not max)
    /// This heals the player instantly
    /// </summary>
    public void UpgradePlayerHealth()
    {
        if (playerHealth == null)
        {
            Debug.LogError("[UpgradeManager] Cannot upgrade health - PlayerHealth not assigned!");
            return;
        }

        healthUpgradeLevel++;
        int healthBoost = 25; // Add 25 HP per upgrade

        // Add health to current health (this heals the player)
        playerHealth.AddHealth(healthBoost);

        Debug.Log($"[UpgradeManager] ✅ HEALTH UPGRADED! Level {healthUpgradeLevel}");
        Debug.Log($"[UpgradeManager] Added {healthBoost} HP to current health");
    }


    public int GetHealthUpgradeLevel() => healthUpgradeLevel;
    public int GetCurrentMaxHealth() => baseMaxHealth + (healthUpgradeLevel * healthUpgradeBonus);

    // ===== GUN DAMAGE UPGRADES =====

    /// <summary>
    /// Upgrade Pistol damage
    /// Formula: basePistolDamage + (pistolDamageLevel * damageUpgradeBonus)
    /// </summary>
    public void UpgradePistolDamage()
    {
        if (playerShooter == null)
        {
            Debug.LogError("[UpgradeManager] Cannot upgrade pistol - PlayerConeShooter not assigned!");
            return;
        }

        pistolDamageLevel++;
        int newDamage = basePistolDamage + (pistolDamageLevel * damageUpgradeBonus);

        // Update weapon damage in WeaponData (pistol is always index 0)
        UpdateWeaponDamage(0, newDamage);

        Debug.Log($"[UpgradeManager] ✅ PISTOL DAMAGE UPGRADED! Level {pistolDamageLevel}");
        Debug.Log($"[UpgradeManager] New Pistol Damage: {newDamage} (Base: {basePistolDamage} + Upgrades: {pistolDamageLevel * damageUpgradeBonus})");
    }

    /// <summary>
    /// Upgrade Shotgun damage
    /// Only works if Shotgun is UNLOCKED
    /// Formula: baseShotgunDamage + (shotgunDamageLevel * damageUpgradeBonus)
    /// </summary>
    public void UpgradeShotgunDamage()
    {
        if (playerShooter == null)
        {
            Debug.LogError("[UpgradeManager] Cannot upgrade shotgun - PlayerConeShooter not assigned!");
            return;
        }

        // Find shotgun weapon index
        int shotgunIndex = FindWeaponIndexByType(WeaponData.WeaponType.Shotgun);

        // Check if shotgun exists
        if (shotgunIndex == -1)
        {
            Debug.LogError("[UpgradeManager] Shotgun not found in weapons array!");
            return;
        }

        // Check if shotgun is unlocked
        if (!playerShooter.IsWeaponUnlocked(shotgunIndex))
        {
            Debug.LogWarning("[UpgradeManager] ❌ Cannot upgrade Shotgun - Shotgun is LOCKED! Unlock it first.");
            return;
        }

        shotgunDamageLevel++;
        int newDamage = baseShotgunDamage + (shotgunDamageLevel * damageUpgradeBonus);

        UpdateWeaponDamage(shotgunIndex, newDamage);

        Debug.Log($"[UpgradeManager] ✅ SHOTGUN DAMAGE UPGRADED! Level {shotgunDamageLevel}");
        Debug.Log($"[UpgradeManager] New Shotgun Damage: {newDamage} (Base: {baseShotgunDamage} + Upgrades: {shotgunDamageLevel * damageUpgradeBonus})");
    }

    /// <summary>
    /// Upgrade Machine Gun damage
    /// Only works if Machine Gun is UNLOCKED
    /// Formula: baseMachineGunDamage + (machineGunDamageLevel * damageUpgradeBonus)
    /// </summary>
    public void UpgradeMachineGunDamage()
    {
        if (playerShooter == null)
        {
            Debug.LogError("[UpgradeManager] Cannot upgrade machine gun - PlayerConeShooter not assigned!");
            return;
        }

        // Find machine gun weapon index (check by name)
        int machineGunIndex = FindWeaponIndexByName("Machine", "AK");

        // Check if machine gun exists
        if (machineGunIndex == -1)
        {
            Debug.LogError("[UpgradeManager] Machine Gun not found in weapons array!");
            return;
        }

        // Check if machine gun is unlocked
        if (!playerShooter.IsWeaponUnlocked(machineGunIndex))
        {
            Debug.LogWarning("[UpgradeManager] ❌ Cannot upgrade Machine Gun - Machine Gun is LOCKED! Unlock it first.");
            return;
        }

        machineGunDamageLevel++;
        int newDamage = baseMachineGunDamage + (machineGunDamageLevel * damageUpgradeBonus);

        UpdateWeaponDamage(machineGunIndex, newDamage);

        Debug.Log($"[UpgradeManager] ✅ MACHINE GUN DAMAGE UPGRADED! Level {machineGunDamageLevel}");
        Debug.Log($"[UpgradeManager] New Machine Gun Damage: {newDamage} (Base: {baseMachineGunDamage} + Upgrades: {machineGunDamageLevel * damageUpgradeBonus})");
    }

    public int GetPistolDamageLevel() => pistolDamageLevel;
    public int GetShotgunDamageLevel() => shotgunDamageLevel;
    public int GetMachineGunDamageLevel() => machineGunDamageLevel;

    public int GetCurrentPistolDamage() => basePistolDamage + (pistolDamageLevel * damageUpgradeBonus);
    public int GetCurrentShotgunDamage() => baseShotgunDamage + (shotgunDamageLevel * damageUpgradeBonus);
    public int GetCurrentMachineGunDamage() => baseMachineGunDamage + (machineGunDamageLevel * damageUpgradeBonus);

    // ===== WEAPON UNLOCK SYSTEM =====

    /// <summary>
    /// Unlock Shotgun weapon
    /// Delegates to PlayerConeShooter
    /// </summary>
    public void UnlockShotgun()
    {
        if (playerShooter == null)
        {
            Debug.LogError("[UpgradeManager] Cannot unlock shotgun - PlayerConeShooter not assigned!");
            return;
        }

        playerShooter.UnlockShotgun();
        Debug.Log("[UpgradeManager] 🔓 Shotgun unlock request sent to PlayerConeShooter");
    }

    /// <summary>
    /// Unlock Machine Gun weapon
    /// Delegates to PlayerConeShooter
    /// </summary>
    public void UnlockMachineGun()
    {
        if (playerShooter == null)
        {
            Debug.LogError("[UpgradeManager] Cannot unlock machine gun - PlayerConeShooter not assigned!");
            return;
        }

        playerShooter.UnlockMachineGun();
        Debug.Log("[UpgradeManager] 🔓 Machine Gun unlock request sent to PlayerConeShooter");
    }

    // ===== HELPER METHODS =====

    /// <summary>
    /// Find weapon index by type
    /// </summary>
    private int FindWeaponIndexByType(WeaponData.WeaponType weaponType)
    {
        WeaponData[] weapons = playerShooter.GetAllWeapons();
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].weaponType == weaponType)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Find weapon index by name (partial match)
    /// </summary>
    private int FindWeaponIndexByName(params string[] names)
    {
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

    /// <summary>
    /// Update weapon damage in the weapon data
    /// </summary>
    private void UpdateWeaponDamage(int weaponIndex, int newDamage)
    {
        WeaponData weapon = playerShooter.GetAllWeapons()[weaponIndex];
        if (weapon != null)
        {
            weapon.SetDamage(newDamage);
        }
    }

    // ===== DEBUG METHODS =====

    /// <summary>
    /// Print all current upgrade stats
    /// </summary>
    public void PrintUpgradeStats()
    {
        Debug.Log("========== UPGRADE STATS ==========");
        Debug.Log("--- SPEED ---");
        Debug.Log($"Level: {GetSpeedUpgradeLevel()} | Current Speed: {GetCurrentPlayerSpeed()}");
        Debug.Log("--- HEALTH ---");
        Debug.Log($"Level: {GetHealthUpgradeLevel()} | Current Max Health: {GetCurrentMaxHealth()}");
        Debug.Log("--- WEAPON DAMAGE ---");
        Debug.Log($"Pistol: Level {GetPistolDamageLevel()} | Damage: {GetCurrentPistolDamage()}");
        Debug.Log($"Shotgun: Level {GetShotgunDamageLevel()} | Damage: {GetCurrentShotgunDamage()}");
        Debug.Log($"Machine Gun: Level {GetMachineGunDamageLevel()} | Damage: {GetCurrentMachineGunDamage()}");
        Debug.Log("==================================");
    }

    /// <summary>
    /// DEBUG: Reset all upgrades to level 0
    /// </summary>
    [ContextMenu("DEBUG - Reset All Upgrades")]
    public void DEBUG_ResetAllUpgrades()
    {
        speedUpgradeLevel = 0;
        healthUpgradeLevel = 0;
        pistolDamageLevel = 0;
        shotgunDamageLevel = 0;
        machineGunDamageLevel = 0;

        Debug.Log("[UpgradeManager] 🔄 All upgrades reset to level 0");
        PrintUpgradeStats();
    }

    /// <summary>
    /// DEBUG: Show weapon unlock status
    /// </summary>
    [ContextMenu("DEBUG - Show Weapon Status")]
    public void DEBUG_ShowWeaponStatus()
    {
        if (playerShooter != null)
        {
            playerShooter.PrintWeaponLockStatus();
        }
    }
}
