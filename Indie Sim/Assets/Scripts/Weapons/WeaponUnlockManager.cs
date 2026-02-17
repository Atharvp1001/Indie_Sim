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

        // Always unlock the first weapon (starter weapon) on scene load
        unlockedWeaponNames.Clear();
        UnlockWeapon(allWeaponsInGame[0]);
        Debug.Log($"[WeaponUnlockManager] 🔄 RESET - Starter weapon unlocked: {allWeaponsInGame[0].weaponName}");

        PrintUnlockStatus();
    }

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
            Debug.LogWarning($"[WeaponUnlockManager] {weapon.weaponName} is already unlocked!");
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
}
