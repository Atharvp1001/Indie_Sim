using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    // ─────────────────────────────────────────────────────────────────
    //  INSPECTOR REFERENCES
    // ─────────────────────────────────────────────────────────────────
    [Header("Player References")]
    [SerializeField] private PlayerController playerMovement;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerConeShooter playerShooter;
    [SerializeField] private WeaponInventory weaponInventory;

    [Header("All Upgrade Templates")]
    [Tooltip("Drag ALL your UpgradeDataSO assets here. The tech tree logic reads from this list.")]
    [SerializeField] private UpgradeDataSO[] allUpgrades;

    [Header("Base Player Speed")]
    [Tooltip("Must match the starting speed in your PlayerController.")]
    [SerializeField] private float basePlayerSpeed = 5f;

    // ─────────────────────────────────────────────────────────────────
    //  RUNTIME BONUS VARIABLES
    //  These are NEVER saved to disk and NEVER touch a ScriptableObject.
    //  They live only while the game is running.
    //  ResetRunData() wipes them all back to zero on death / new run.
    // ─────────────────────────────────────────────────────────────────
    private int bonusPistolDamage;
    private int bonusPistolAmmo;
    private int bonusShotgunDamage;
    private int bonusShotgunAmmo;
    private int bonusMachineGunDamage;
    private int bonusMachineGunAmmo;
    private float bonusSpeed;
    private float bonusStompRadius;
    private int bonusCoinCapacity;
    public int currentDungeonLevel; // This is set by the DungeonManager each time you enter a new floor, and read by UpgradeManager when filtering available upgrades.

    // ═════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═════════════════════════════════════════════════════════════════
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (weaponInventory == null)
            weaponInventory = FindObjectOfType<WeaponInventory>();

        ValidateReferences();

        // Always start a fresh run with zeroed bonuses.
        // When the player dies, the death handler calls ResetRunData() explicitly.
        ResetRunData();
    }

    // ═════════════════════════════════════════════════════════════════
    //  CORE: APPLY UPGRADE
    //  This is the ONE method the ShopUI / StoreManager calls.
    //  Pass in an UpgradeDataSO and this method handles everything.
    // ═════════════════════════════════════════════════════════════════
    public void ApplyUpgrade(UpgradeDataSO upgrade)
    {
        if (upgrade == null)
        {
            Debug.LogError("[UpgradeManager] ApplyUpgrade received a null upgrade!");
            return;
        }

        Debug.Log($"[UpgradeManager] ✅ Applying: {upgrade.upgradeName}");

        switch (upgrade.upgradeType)
        {
            case UpgradeDataSO.UpgradeType.Speed:
                HandleSpeedUpgrade(upgrade);
                break;

            case UpgradeDataSO.UpgradeType.UnlockGun:
                HandleUnlockGun(upgrade);
                break;

            case UpgradeDataSO.UpgradeType.GunUpgrade:
                HandleGunUpgrade(upgrade);
                break;

            case UpgradeDataSO.UpgradeType.CoinPurse:
                bonusCoinCapacity += upgrade.capacityIncrease;
                Debug.Log($"[UpgradeManager] Coin capacity bonus → +{bonusCoinCapacity} total");
                break;

            case UpgradeDataSO.UpgradeType.StompUpgrade:
                bonusStompRadius += upgrade.radiusIncrease;
                Debug.Log($"[UpgradeManager] Stomp radius bonus → +{bonusStompRadius} total");
                break;

            default:
                Debug.LogWarning($"[UpgradeManager] Unhandled upgrade type: {upgrade.upgradeType}");
                break;
        }

        PrintUpgradeStats();
    }

    // ─── Handlers called by the switch above ──────────────────────────

    private void HandleSpeedUpgrade(UpgradeDataSO upgrade)
    {
        bonusSpeed += upgrade.speedIncrease;

        if (playerMovement != null)
            playerMovement.SetSpeed(basePlayerSpeed + bonusSpeed);

        Debug.Log($"[UpgradeManager] Speed bonus → +{bonusSpeed} | Final speed: {basePlayerSpeed + bonusSpeed}");
    }

    private void HandleUnlockGun(UpgradeDataSO upgrade)
    {
        switch (upgrade.targetWeapon)
        {
            case UpgradeDataSO.TargetWeapon.Shotgun:
                UnlockShotgun();
                break;

            case UpgradeDataSO.TargetWeapon.MachineGun:
                UnlockMachineGun();
                break;

            default:
                Debug.LogWarning($"[UpgradeManager] UnlockGun has no valid target: {upgrade.targetWeapon}");
                break;
        }
    }

    private void HandleGunUpgrade(UpgradeDataSO upgrade)
    {
        // A single card CAN have both damageIncrease and ammoIncrease filled in —
        // both get applied here. Fields left at 0 simply add nothing.
        switch (upgrade.targetWeapon)
        {
            case UpgradeDataSO.TargetWeapon.Pistol:
                bonusPistolDamage += upgrade.damageIncrease;
                bonusPistolAmmo += upgrade.ammoIncrease;
                Debug.Log($"[UpgradeManager] Pistol → Dmg bonus: +{bonusPistolDamage} | Ammo bonus: +{bonusPistolAmmo}");
                break;

            case UpgradeDataSO.TargetWeapon.Shotgun:
                bonusShotgunDamage += upgrade.damageIncrease;
                bonusShotgunAmmo += upgrade.ammoIncrease;
                Debug.Log($"[UpgradeManager] Shotgun → Dmg bonus: +{bonusShotgunDamage} | Ammo bonus: +{bonusShotgunAmmo}");
                break;

            case UpgradeDataSO.TargetWeapon.MachineGun:
                bonusMachineGunDamage += upgrade.damageIncrease;
                bonusMachineGunAmmo += upgrade.ammoIncrease;
                Debug.Log($"[UpgradeManager] MachineGun → Dmg bonus: +{bonusMachineGunDamage} | Ammo bonus: +{bonusMachineGunAmmo}");
                break;

            default:
                Debug.LogWarning($"[UpgradeManager] GunUpgrade has no valid target weapon set on the SO!");
                break;
        }
    }

    // ═════════════════════════════════════════════════════════════════
    //  TECH TREE: GET AVAILABLE UPGRADES
    //  Call this from ShopUI when building the upgrade card pool.
    //  Pass in the current dungeon floor number (1, 2, 3...).
    //
    //  Rules:
    //   - All upgrades with minDungeonLevel <= currentDungeonLevel are eligible.
    //   - Shotgun Upgrade is HIDDEN until Shotgun is unlocked.
    //   - Unlock Machine Gun is HIDDEN until dungeon level >= 4.
    //   - Machine Gun Upgrade is HIDDEN until Machine Gun is unlocked.
    //   - "Unlock X" cards disappear once that weapon is already unlocked.
    // ═════════════════════════════════════════════════════════════════
    public List<UpgradeDataSO> GetAvailableUpgrades()
    {
        List<UpgradeDataSO> available = new List<UpgradeDataSO>();

        foreach (UpgradeDataSO upgrade in allUpgrades)
        {
            if (upgrade == null) continue;

            // Filter 1: Dungeon level requirement
            if (currentDungeonLevel < upgrade.minDungeonLevel) continue;

            // Filter 2: Skip unlock cards for already-unlocked weapons
            if (upgrade.upgradeType == UpgradeDataSO.UpgradeType.UnlockGun)
            {
                // ✅ FIX: call through WeaponUnlockManager, not locally
                WeaponData weapon = FindWeaponForTargetEnum(upgrade.targetWeapon);
                if (weapon != null && WeaponUnlockManager.Instance.IsWeaponUnlocked(weapon))
                    continue;
            }

            available.Add(upgrade);
        }

        return available;
    }

    // Helper to resolve a TargetWeapon enum → actual WeaponData from inventory
    private WeaponData FindWeaponForTargetEnum(UpgradeDataSO.TargetWeapon target)
    {
        if (weaponInventory == null) return null;

        foreach (WeaponData w in weaponInventory.GetAllWeapons())
        {
            if (w == null) continue;

            switch (target)
            {
                case UpgradeDataSO.TargetWeapon.Shotgun:
                    if (w.weaponType == WeaponData.WeaponType.Shotgun) return w;
                    break;

                case UpgradeDataSO.TargetWeapon.MachineGun:
                    if (w.weaponName.ToLower().Contains("machine") ||
                        w.weaponName.ToLower().Contains("ak")) return w;
                    break;

                case UpgradeDataSO.TargetWeapon.Pistol:
                    if (w.weaponType == WeaponData.WeaponType.Standard ||
                        w.weaponType == WeaponData.WeaponType.Piercer) return w;
                    break;
            }
        }
        return null;
    }

    // ═════════════════════════════════════════════════════════════════
    //  RESET (call this on player death / new run start)
    // ═════════════════════════════════════════════════════════════════
    public void ResetRunData()
    {
        bonusPistolDamage = 0;
        bonusPistolAmmo = 0;
        bonusShotgunDamage = 0;
        bonusShotgunAmmo = 0;
        bonusMachineGunDamage = 0;
        bonusMachineGunAmmo = 0;
        bonusSpeed = 0f;
        bonusStompRadius = 0f;
        bonusCoinCapacity = 0;

        // Restore speed to base
        if (playerMovement != null)
            playerMovement.SetSpeed(basePlayerSpeed);

        Debug.Log("[UpgradeManager] 🔄 Run data reset — all bonuses cleared to zero.");
        PrintUpgradeStats();
    }

    // ═════════════════════════════════════════════════════════════════
    //  STAT GETTERS
    //  PlayerConeShooter, AmmoSystem, StompAbility etc. call these
    //  to get the REAL current stat = (base from SO) + (bonus from run).
    //  The SO is never modified — the bonus lives only here.
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns baseDamagePerShot from the WeaponData SO + any bonus earned this run.
    /// PlayerConeShooter should call this instead of reading weapon.baseDamagePerShot directly.
    /// </summary>
    public int GetFinalDamage(WeaponData weapon)
    {
        if (weapon == null) return 0;
        return weapon.baseDamagePerShot + GetBonusDamageFor(weapon);
    }

    /// <summary>
    /// Returns magazineCapacity from the WeaponData SO + any bonus earned this run.
    /// </summary>
    public int GetFinalAmmo(WeaponData weapon)
    {
        if (weapon == null) return 0;
        return weapon.magazineCapacity + GetBonusAmmoFor(weapon);
    }


    /// <summary>
    /// Returns the weapon's final pierce count = base (from SO) + any pierce bonus this run.
    /// Currently returns base only. Wire up bonusPistolPierceCount here when you make a Pierce upgrade card.
    /// </summary>
    public int GetFinalPierceCount(WeaponData weapon)
    {
        if (weapon == null) return 0;
        return weapon.maxPierceCount + GetBonusPierceCountFor(weapon);
    }

    private int GetBonusPierceCountFor(WeaponData weapon)
    {
        // No pierce-specific upgrade card exists yet, so this returns 0.
        // When you create one, add a bonusPistolPierceCount variable at the top of this class
        // and increment it here, exactly like bonusPistolDamage.
        return 0;
    }

    public float GetFinalSpeed() => basePlayerSpeed + bonusSpeed;
    public float GetFinalStompRadius(float baseRadius) => baseRadius + bonusStompRadius;
    public int GetFinalCoinCapacity(int baseCapacity) => baseCapacity + bonusCoinCapacity;

    // ─── Internal helpers to map a WeaponData asset → the right bonus pool ──
    private int GetBonusDamageFor(WeaponData weapon)
    {
        if (weapon.weaponType == WeaponData.WeaponType.Piercer ||
            weapon.weaponName.ToLower().Contains("pistol"))
            return bonusPistolDamage;

        if (weapon.weaponType == WeaponData.WeaponType.Shotgun)
            return bonusShotgunDamage;

        if (weapon.weaponName.ToLower().Contains("machine") ||
            weapon.weaponName.ToLower().Contains("ak"))
            return bonusMachineGunDamage;

        return 0;
    }

    private int GetBonusAmmoFor(WeaponData weapon)
    {
        if (weapon.weaponType == WeaponData.WeaponType.Piercer ||
            weapon.weaponName.ToLower().Contains("pistol"))
            return bonusPistolAmmo;

        if (weapon.weaponType == WeaponData.WeaponType.Shotgun)
            return bonusShotgunAmmo;

        if (weapon.weaponName.ToLower().Contains("machine") ||
            weapon.weaponName.ToLower().Contains("ak"))
            return bonusMachineGunAmmo;

        return 0;
    }

    // ═════════════════════════════════════════════════════════════════
    //  WEAPON UNLOCK LOGIC
    // ═════════════════════════════════════════════════════════════════
    public void UnlockShotgun()
    {
        if (WeaponUnlockManager.Instance == null || weaponInventory == null) return;

        int index = FindWeaponIndexByType(WeaponData.WeaponType.Shotgun);
        if (index == -1) { Debug.LogError("[UpgradeManager] Shotgun not found in WeaponInventory!"); return; }

        WeaponData shotgun = weaponInventory.GetAllWeapons()[index];
        if (WeaponUnlockManager.Instance.IsWeaponUnlocked(shotgun))
        {
            Debug.LogWarning("[UpgradeManager] Shotgun is already unlocked.");
            return;
        }

        WeaponUnlockManager.Instance.UnlockWeapon(shotgun);
        Debug.Log("[UpgradeManager] 🔓 Shotgun unlocked!");

       
    }

    public void UnlockMachineGun()
    {
        if (WeaponUnlockManager.Instance == null || weaponInventory == null) return;

        int index = FindWeaponIndexByName("Machine", "AK");
        if (index == -1) { Debug.LogError("[UpgradeManager] Machine Gun not found in WeaponInventory!"); return; }

        WeaponData machineGun = weaponInventory.GetAllWeapons()[index];
        if (WeaponUnlockManager.Instance.IsWeaponUnlocked(machineGun))
        {
            Debug.LogWarning("[UpgradeManager] Machine Gun is already unlocked.");
            return;
        }

        WeaponUnlockManager.Instance.UnlockWeapon(machineGun);
        Debug.Log("[UpgradeManager] 🔓 Machine Gun unlocked!");

        
    }

    private bool IsShotgunUnlocked()
    {
        if (WeaponUnlockManager.Instance == null || weaponInventory == null) return false;
        int index = FindWeaponIndexByType(WeaponData.WeaponType.Shotgun);
        if (index == -1) return false;
        return WeaponUnlockManager.Instance.IsWeaponUnlocked(weaponInventory.GetAllWeapons()[index]);
    }

    private bool IsMachineGunUnlocked()
    {
        if (WeaponUnlockManager.Instance == null || weaponInventory == null) return false;
        int index = FindWeaponIndexByName("Machine", "AK");
        if (index == -1) return false;
        return WeaponUnlockManager.Instance.IsWeaponUnlocked(weaponInventory.GetAllWeapons()[index]);
    }

    // ═════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ═════════════════════════════════════════════════════════════════
    private int FindWeaponIndexByType(WeaponData.WeaponType type)
    {
        if (weaponInventory == null) return -1;
        WeaponData[] weapons = weaponInventory.GetAllWeapons();
        for (int i = 0; i < weapons.Length; i++)
            if (weapons[i].weaponType == type) return i;
        return -1;
    }

    private int FindWeaponIndexByName(params string[] names)
    {
        if (weaponInventory == null) return -1;
        WeaponData[] weapons = weaponInventory.GetAllWeapons();
        for (int i = 0; i < weapons.Length; i++)
            foreach (string n in names)
                if (weapons[i].weaponName.ToLower().Contains(n.ToLower())) return i;
        return -1;
    }

    private void ValidateReferences()
    {
        if (playerMovement == null) Debug.LogError("[UpgradeManager] PlayerController not assigned!");
        if (playerHealth == null) Debug.LogError("[UpgradeManager] PlayerHealth not assigned!");
        if (playerShooter == null) Debug.LogError("[UpgradeManager] PlayerConeShooter not assigned!");
        if (weaponInventory == null) Debug.LogError("[UpgradeManager] WeaponInventory not assigned!");
        if (allUpgrades == null || allUpgrades.Length == 0)
            Debug.LogWarning("[UpgradeManager] No UpgradeDataSO assets assigned in allUpgrades[]!");
    }

    // ═════════════════════════════════════════════════════════════════
    //  DEBUG
    // ═════════════════════════════════════════════════════════════════
    public void PrintUpgradeStats()
    {
        Debug.Log("═══════════ UPGRADE BONUSES THIS RUN ═══════════");
        Debug.Log($"Speed:      +{bonusSpeed}  →  Final: {GetFinalSpeed()}");
        Debug.Log($"Pistol:     Dmg +{bonusPistolDamage}  | Ammo +{bonusPistolAmmo}");
        Debug.Log($"Shotgun:    Dmg +{bonusShotgunDamage}  | Ammo +{bonusShotgunAmmo}");
        Debug.Log($"MachineGun: Dmg +{bonusMachineGunDamage}  | Ammo +{bonusMachineGunAmmo}");
        Debug.Log($"Stomp Radius bonus: +{bonusStompRadius}");
        Debug.Log($"Coin Capacity bonus: +{bonusCoinCapacity}");
        Debug.Log("═════════════════════════════════════════════════");
    }

    [ContextMenu("DEBUG - Reset Run Data")]
    public void DEBUG_ResetRunData() => ResetRunData();

    [ContextMenu("DEBUG - Show Weapon Unlock Status")]
    public void DEBUG_ShowWeaponStatus() => WeaponUnlockManager.Instance?.PrintUnlockStatus();
}