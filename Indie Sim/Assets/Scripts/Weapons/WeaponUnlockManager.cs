using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple database that tracks which weapons are unlocked this run.
/// UpgradeManager tells it what to unlock, this just stores the state.
/// </summary>
public class WeaponUnlockManager : MonoBehaviour
{
    [Header("Weapon Database")]
    [SerializeField] private WeaponData[] allWeaponsInGame;

    [Header("Unlock Status (Per-Run)")]
    [SerializeField] private List<string> unlockedWeaponNames = new List<string>();

    [Header("⚠️ DEBUG / TESTING OPTIONS")]
    [SerializeField] private bool unlockAllWeaponsOnStart = false;
    [Tooltip("Check specific weapons to unlock at start (for testing)")]
    [SerializeField] private List<WeaponData> debugUnlockedWeapons = new List<WeaponData>();

    public static WeaponUnlockManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[WeaponUnlockManager] Multiple instances detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeUnlockSystem();
    }

    private void InitializeUnlockSystem()
    {
        if (allWeaponsInGame == null || allWeaponsInGame.Length == 0)
        {
            Debug.LogError("[WeaponUnlockManager] No weapons assigned to allWeaponsInGame array!");
            return;
        }

        // Clear previous unlocks
        unlockedWeaponNames.Clear();

        // ✅ OPTION 1: Unlock all weapons (testing mode)
        if (unlockAllWeaponsOnStart)
        {
            Debug.Log("[WeaponUnlockManager] 🔧 DEBUG MODE: Unlocking ALL weapons");
            foreach (WeaponData weapon in allWeaponsInGame)
            {
                UnlockWeapon(weapon);
            }
            PrintUnlockStatus();
            return;
        }

        // ✅ OPTION 2: Unlock specific weapons from debug list
        if (debugUnlockedWeapons != null && debugUnlockedWeapons.Count > 0)
        {
            Debug.Log($"[WeaponUnlockManager] 🔧 DEBUG MODE: Unlocking {debugUnlockedWeapons.Count} specific weapons");
            foreach (WeaponData weapon in debugUnlockedWeapons)
            {
                if (weapon != null)
                {
                    UnlockWeapon(weapon);
                }
            }
            PrintUnlockStatus();
            return;
        }

        // ✅ NORMAL MODE: Only unlock starter weapon
        UnlockWeapon(allWeaponsInGame[0]);
        Debug.Log($"[WeaponUnlockManager] 🔄 RESET - Starter weapon unlocked: {allWeaponsInGame[0].weaponName}");

        PrintUnlockStatus();
    }

    /// <summary>
    /// Call at the start of each new run to clear unlocks back to just the
    /// starter weapon. Named to match CoinManager/EnemyKillTracker's sibling
    /// reset methods (temporary bridge from GameManager.StartNewRun(), Phase 4).
    /// </summary>
    public void ResetForNewRun() => InitializeUnlockSystem();

    /// <summary>
    /// ✅ CORE METHOD: Unlocks a weapon by adding it to the list.
    /// This is the ONLY method UpgradeManager should call.
    /// </summary>
    public void UnlockWeapon(WeaponData weapon)
    {
        if (weapon == null)
        {
            Debug.LogError("[WeaponUnlockManager] Cannot unlock a null weapon!");
            return;
        }

        if (unlockedWeaponNames.Contains(weapon.weaponName))
        {
            // Don't spam warnings during debug unlock
            if (!unlockAllWeaponsOnStart && !debugUnlockedWeapons.Contains(weapon))
            {
                Debug.LogWarning($"[WeaponUnlockManager] {weapon.weaponName} is already unlocked!");
            }
            return;
        }

        unlockedWeaponNames.Add(weapon.weaponName);
        Debug.Log($"[WeaponUnlockManager] ✅ UNLOCKED: {weapon.weaponName} (THIS RUN ONLY)");
    }

    /// <summary>
    /// Checks if a weapon is unlocked.
    /// </summary>
    public bool IsWeaponUnlocked(WeaponData weapon)
    {
        if (weapon == null) return false;
        return unlockedWeaponNames.Contains(weapon.weaponName);
    }

    public bool IsWeaponUnlockedByName(string weaponName)
    {
        return unlockedWeaponNames.Contains(weaponName);
    }

    public int GetUnlockedWeaponCount()
    {
        return unlockedWeaponNames.Count;
    }

    public int GetTotalWeaponCount()
    {
        return allWeaponsInGame.Length;
    }

    public void PrintUnlockStatus()
    {
        Debug.Log("========== WEAPON UNLOCK STATUS (THIS RUN) ==========");
        foreach (WeaponData weapon in allWeaponsInGame)
        {
            string status = IsWeaponUnlocked(weapon) ? "🔓 UNLOCKED" : "🔒 LOCKED";
            Debug.Log($"{weapon.weaponName} - {status}");
        }
        Debug.Log($"Total Unlocked: {GetUnlockedWeaponCount()}/{GetTotalWeaponCount()}");
        Debug.Log("====================================================");
    }

    // ===== DEBUG METHODS =====

    /// <summary>
    /// Runtime method to unlock all weapons (can be called from console or debug menu)
    /// </summary>
    [ContextMenu("DEBUG - Unlock All Weapons")]
    public void DEBUG_UnlockAllWeapons()
    {
        Debug.Log("[WeaponUnlockManager] 🔧 DEBUG: Unlocking all weapons at runtime");
        foreach (WeaponData weapon in allWeaponsInGame)
        {
            UnlockWeapon(weapon);
        }
        PrintUnlockStatus();


    }

    /// <summary>
    /// Runtime method to unlock a specific weapon by name
    /// </summary>
    [ContextMenu("DEBUG - Unlock Shotgun")]
    public void DEBUG_UnlockShotgun()
    {
        WeaponData shotgun = System.Array.Find(allWeaponsInGame, w => w.weaponType == WeaponData.WeaponType.Shotgun);
        if (shotgun != null)
        {
            UnlockWeapon(shotgun);
            
        }
    }

    [ContextMenu("DEBUG - Unlock Machine Gun")]
    public void DEBUG_UnlockMachineGun()
    {
        WeaponData mg = System.Array.Find(allWeaponsInGame, w =>
            w.weaponName.ToLower().Contains("machine") ||
            w.weaponName.ToLower().Contains("ak"));
        if (mg != null)
        {
            UnlockWeapon(mg);
            
        }
    }
}
