using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerController playerMovement;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerConeShooter playerShooter;
    [SerializeField] private WeaponInventory weaponInventory;

    [Header("Speed Upgrade")]
    [SerializeField] private float basePlayerSpeed = 5f;
    [SerializeField] private float speedUpgradeBonus = 1f;
    private int speedUpgradeLevel = 0;

    [Header("Gun Damage Upgrade")]
    [SerializeField] private int basePistolDamage = 10;
    [SerializeField] private int baseShotgunDamage = 25;
    [SerializeField] private int baseMachineGunDamage = 8;
    [SerializeField] private int damageUpgradeBonus = 2;
    private int pistolDamageLevel = 0;
    private int shotgunDamageLevel = 0;
    private int machineGunDamageLevel = 0;

    [Header("Piercer Upgrade (Pistol)")]
    [SerializeField] private int basePistolPierceCount = 2; // Starting pierce count
    [SerializeField] private int maxPistolPierceCount = 5;   // Maximum pierce count
    [SerializeField] private int pierceIncreasePerUpgrade = 1; // How much to increase per upgrade

    private void Start()
    {
        if (weaponInventory == null)
            weaponInventory = FindObjectOfType<WeaponInventory>();

        ValidateReferences();
        ResetAllWeaponDamages();

        Debug.Log("[UpgradeManager] 🔄 RESET - All upgrades cleared for new run");
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
        if (weaponInventory == null)
            Debug.LogError("[UpgradeManager] WeaponInventory reference not assigned!");
    }

    /// <summary>
    /// ✅ UPDATED: Resets weapon damages AND pierce counts on scene load.
    /// </summary>
    private void ResetAllWeaponDamages()
    {
        if (weaponInventory == null) return;

        WeaponData[] weapons = weaponInventory.GetAllWeapons();

        // Reset Pistol (index 0)
        if (weapons.Length > 0)
        {
            weapons[0].SetDamage(basePistolDamage);
            weapons[0].maxPierceCount = basePistolPierceCount; 
            Debug.Log($"[UpgradeManager] Reset Pistol damage to {basePistolDamage}, pierce to {basePistolPierceCount}");
        }

        // Reset Shotgun
        int shotgunIndex = FindWeaponIndexByType(WeaponData.WeaponType.Shotgun);
        if (shotgunIndex != -1)
        {
            weapons[shotgunIndex].SetDamage(baseShotgunDamage);
            Debug.Log($"[UpgradeManager] Reset Shotgun damage to {baseShotgunDamage}");
        }

        // Reset Machine Gun
        int mgIndex = FindWeaponIndexByName("Machine", "AK");
        if (mgIndex != -1)
        {
            weapons[mgIndex].SetDamage(baseMachineGunDamage);
            Debug.Log($"[UpgradeManager] Reset Machine Gun damage to {baseMachineGunDamage}");
        }
    }


    // ===== SPEED UPGRADES =====

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
        Debug.Log($"[UpgradeManager] New Speed: {newSpeed}");
    }

    public int GetSpeedUpgradeLevel() => speedUpgradeLevel;
    public float GetCurrentPlayerSpeed() => basePlayerSpeed + (speedUpgradeLevel * speedUpgradeBonus);

    // ===== GUN DAMAGE UPGRADES =====

    /// <summary>
    /// Upgrades pistol damage AND pierce count (since pistol is a piercer weapon).
    /// Each upgrade increases:
    /// - Damage by damageUpgradeBonus
    /// - Pierce count by pierceIncreasePerUpgrade (up to max)
    /// </summary>
    public void UpgradePistolDamage()
    {
        if (weaponInventory == null)
        {
            Debug.LogError("[UpgradeManager] Cannot upgrade pistol - WeaponInventory not assigned!");
            return;
        }

        pistolDamageLevel++;

        // ✅ UPGRADE DAMAGE
        int newDamage = basePistolDamage + (pistolDamageLevel * damageUpgradeBonus);
        UpdateWeaponDamage(0, newDamage);

        // ✅ UPGRADE PIERCE COUNT
        int newPierceCount = basePistolPierceCount + (pistolDamageLevel * pierceIncreasePerUpgrade);
        newPierceCount = Mathf.Min(newPierceCount, maxPistolPierceCount); // Cap at max
        UpdateWeaponPierceCount(0, newPierceCount);

        Debug.Log($"[UpgradeManager] ✅ PISTOL UPGRADED! Level {pistolDamageLevel}");
        Debug.Log($"[UpgradeManager] New Damage: {newDamage} | Pierce Count: {newPierceCount}");
    }


    public void UpgradeShotgunDamage()
    {
        if (weaponInventory == null)
        {
            Debug.LogError("[UpgradeManager] Cannot upgrade shotgun - WeaponInventory not assigned!");
            return;
        }

        int shotgunIndex = FindWeaponIndexByType(WeaponData.WeaponType.Shotgun);

        if (shotgunIndex == -1)
        {
            Debug.LogError("[UpgradeManager] Shotgun not found in weapons array!");
            return;
        }

        WeaponData shotgun = weaponInventory.GetAllWeapons()[shotgunIndex];
        if (!WeaponUnlockManager.Instance.IsWeaponUnlocked(shotgun))
        {
            Debug.LogWarning("[UpgradeManager] ❌ Cannot upgrade Shotgun - Shotgun is LOCKED!");
            return;
        }

        shotgunDamageLevel++;
        int newDamage = baseShotgunDamage + (shotgunDamageLevel * damageUpgradeBonus);
        UpdateWeaponDamage(shotgunIndex, newDamage);

        Debug.Log($"[UpgradeManager] ✅ SHOTGUN DAMAGE UPGRADED! Level {shotgunDamageLevel}");
        Debug.Log($"[UpgradeManager] New Shotgun Damage: {newDamage}");
    }

    public void UpgradeMachineGunDamage()
    {
        if (weaponInventory == null)
        {
            Debug.LogError("[UpgradeManager] Cannot upgrade machine gun - WeaponInventory not assigned!");
            return;
        }

        int machineGunIndex = FindWeaponIndexByName("Machine", "AK");

        if (machineGunIndex == -1)
        {
            Debug.LogError("[UpgradeManager] Machine Gun not found in weapons array!");
            return;
        }

        WeaponData machineGun = weaponInventory.GetAllWeapons()[machineGunIndex];
        if (!WeaponUnlockManager.Instance.IsWeaponUnlocked(machineGun))
        {
            Debug.LogWarning("[UpgradeManager] ❌ Cannot upgrade Machine Gun - Machine Gun is LOCKED!");
            return;
        }

        machineGunDamageLevel++;
        int newDamage = baseMachineGunDamage + (machineGunDamageLevel * damageUpgradeBonus);
        UpdateWeaponDamage(machineGunIndex, newDamage);

        Debug.Log($"[UpgradeManager] ✅ MACHINE GUN DAMAGE UPGRADED! Level {machineGunDamageLevel}");
        Debug.Log($"[UpgradeManager] New Machine Gun Damage: {newDamage}");
    }

    public int GetPistolDamageLevel() => pistolDamageLevel;
    public int GetShotgunDamageLevel() => shotgunDamageLevel;
    public int GetMachineGunDamageLevel() => machineGunDamageLevel;

    // ✅ NEW: Helper to update pierce count
    /// <summary>
    /// Updates a weapon's max pierce count (for piercer weapons).
    /// </summary>
    private void UpdateWeaponPierceCount(int weaponIndex, int newPierceCount)
    {
        if (weaponInventory == null) return;

        WeaponData weapon = weaponInventory.GetAllWeapons()[weaponIndex];
        if (weapon != null)
        {
            weapon.maxPierceCount = newPierceCount;
            Debug.Log($"[UpgradeManager] {weapon.weaponName} pierce count updated to {newPierceCount}");
        }
    }

    public int GetCurrentPistolDamage() => basePistolDamage + (pistolDamageLevel * damageUpgradeBonus);
    public int GetCurrentPistolPierceCount()
    {
        int pierceCount = basePistolPierceCount + (pistolDamageLevel * pierceIncreasePerUpgrade);
        return Mathf.Min(pierceCount, maxPistolPierceCount);
    }
    public int GetCurrentShotgunDamage() => baseShotgunDamage + (shotgunDamageLevel * damageUpgradeBonus);
    public int GetCurrentMachineGunDamage() => baseMachineGunDamage + (machineGunDamageLevel * damageUpgradeBonus);

    // ===== WEAPON UNLOCK SYSTEM =====

    /// <summary>
    /// ✅ IMPROVED: Finds shotgun and unlocks it.
    /// This is the ONLY place unlock logic exists.
    /// </summary>
    public void UnlockShotgun()
    {
        if (WeaponUnlockManager.Instance == null)
        {
            Debug.LogError("[UpgradeManager] WeaponUnlockManager not found!");
            return;
        }

        if (weaponInventory == null)
        {
            Debug.LogError("[UpgradeManager] WeaponInventory not found!");
            return;
        }

        // Find the shotgun weapon
        int shotgunIndex = FindWeaponIndexByType(WeaponData.WeaponType.Shotgun);

        if (shotgunIndex == -1)
        {
            Debug.LogError("[UpgradeManager] Shotgun not found in weapons array!");
            return;
        }

        WeaponData shotgun = weaponInventory.GetAllWeapons()[shotgunIndex];

        // Check if already unlocked
        if (WeaponUnlockManager.Instance.IsWeaponUnlocked(shotgun))
        {
            Debug.LogWarning("[UpgradeManager] Shotgun is already unlocked!");
            return;
        }

        // ✅ Unlock it in the manager
        WeaponUnlockManager.Instance.UnlockWeapon(shotgun);

        Debug.Log("[UpgradeManager] 🔓 Shotgun unlocked FOR THIS RUN ONLY");

        // ✅ Notify store manager to add shotgun upgrade button to pool
        StoreManager storeManager = FindObjectOfType<StoreManager>();
        if (storeManager != null)
        {
            storeManager.UnlockShotgunUpgrade();
        }

        // ✅ Print current unlock status
        WeaponUnlockManager.Instance.PrintUnlockStatus();
    }

    /// <summary>
    /// ✅ IMPROVED: Finds machine gun and unlocks it.
    /// </summary>
    public void UnlockMachineGun()
    {
        if (WeaponUnlockManager.Instance == null)
        {
            Debug.LogError("[UpgradeManager] WeaponUnlockManager not found!");
            return;
        }

        if (weaponInventory == null)
        {
            Debug.LogError("[UpgradeManager] WeaponInventory not found!");
            return;
        }

        // Find the machine gun weapon
        int mgIndex = FindWeaponIndexByName("Machine", "AK");

        if (mgIndex == -1)
        {
            Debug.LogError("[UpgradeManager] Machine Gun not found in weapons array!");
            return;
        }

        WeaponData machineGun = weaponInventory.GetAllWeapons()[mgIndex];

        // Check if already unlocked
        if (WeaponUnlockManager.Instance.IsWeaponUnlocked(machineGun))
        {
            Debug.LogWarning("[UpgradeManager] Machine Gun is already unlocked!");
            return;
        }

        // ✅ Unlock it in the manager
        WeaponUnlockManager.Instance.UnlockWeapon(machineGun);

        Debug.Log("[UpgradeManager] 🔓 Machine Gun unlocked FOR THIS RUN ONLY");

        // ✅ Notify store manager to add machine gun upgrade button to pool
        StoreManager storeManager = FindObjectOfType<StoreManager>();
        if (storeManager != null)
        {
            storeManager.UnlockMachineGunUpgrade();
        }

        // ✅ Print current unlock status
        WeaponUnlockManager.Instance.PrintUnlockStatus();
    }

    // ===== HELPER METHODS =====

    private int FindWeaponIndexByType(WeaponData.WeaponType weaponType)
    {
        if (weaponInventory == null) return -1;

        WeaponData[] weapons = weaponInventory.GetAllWeapons();
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].weaponType == weaponType)
                return i;
        }
        return -1;
    }

    private int FindWeaponIndexByName(params string[] names)
    {
        if (weaponInventory == null) return -1;

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

    private void UpdateWeaponDamage(int weaponIndex, int newDamage)
    {
        if (weaponInventory == null) return;

        WeaponData weapon = weaponInventory.GetAllWeapons()[weaponIndex];
        if (weapon != null)
        {
            weapon.SetDamage(newDamage);
        }
    }

    // ===== DEBUG METHODS =====

    public void PrintUpgradeStats()
    {
        Debug.Log("========== UPGRADE STATS (THIS RUN) ==========");
        Debug.Log("--- SPEED ---");
        Debug.Log($"Level: {GetSpeedUpgradeLevel()} | Current Speed: {GetCurrentPlayerSpeed()}");
        Debug.Log("--- GUN DAMAGE ---");
        Debug.Log($"Pistol: Level {GetPistolDamageLevel()} | Damage: {GetCurrentPistolDamage()}");
        Debug.Log($"Shotgun: Level {GetShotgunDamageLevel()} | Damage: {GetCurrentShotgunDamage()}");
        Debug.Log($"Machine Gun: Level {GetMachineGunDamageLevel()} | Damage: {GetCurrentMachineGunDamage()}");
        Debug.Log("==============================================");
    }

    [ContextMenu("DEBUG - Reset All Upgrades")]
    public void DEBUG_ResetAllUpgrades()
    {
        speedUpgradeLevel = 0;
        pistolDamageLevel = 0;
        shotgunDamageLevel = 0;
        machineGunDamageLevel = 0;

        ResetAllWeaponDamages();

        Debug.Log("[UpgradeManager] 🔄 All upgrades reset to level 0");
        PrintUpgradeStats();
    }

    [ContextMenu("DEBUG - Show Weapon Status")]
    public void DEBUG_ShowWeaponStatus()
    {
        if (WeaponUnlockManager.Instance != null)
        {
            WeaponUnlockManager.Instance.PrintUnlockStatus();
        }
    }

    public float GetSpeedUpgradeBonus() => speedUpgradeBonus;
    public int GetDamageUpgradeBonus() => damageUpgradeBonus;
}
