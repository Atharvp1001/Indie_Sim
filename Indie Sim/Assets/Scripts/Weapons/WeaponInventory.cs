using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the player's current weapon loadout and handles switching between weapons.
/// This is the "backpack" - it knows what guns you're carrying and which one is active.
/// Supports Tab key and Mouse Scroll Wheel for switching.
/// </summary>
public class WeaponInventory : MonoBehaviour
{
    [Header("Weapon Loadout")]
    [SerializeField] private WeaponData[] availableWeapons; // The guns you're carrying this run
    [SerializeField] private int currentWeaponIndex = 0;

    [Header("Input Settings")]
    [SerializeField] private float scrollThreshold = 0.1f; // Minimum scroll value to register

    private WeaponData currentWeapon;
    private PlayerControls inputActions;

    // ✅ EVENT: Other scripts can listen to this without needing a reference to this script
    public static event Action<WeaponData> OnWeaponChanged;

    #region Unity Lifecycle

    private void Awake()
    {
        // Set up input system for weapon switching
        inputActions = new PlayerControls();

        // Tab key - cycles to next weapon
        inputActions.Player.SwitchWeapon.performed += ctx => SwitchToNextWeapon();

        // Mouse scroll wheel - scrolls through weapons
        inputActions.Player.SwitchWeaponScroll.performed += OnScrollWeapon;
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Start()
    {
        // Find the first unlocked weapon and equip it
        InitializeStartingWeapon();
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// Handles mouse scroll wheel input for weapon switching.
    /// Scroll up = next weapon, Scroll down = previous weapon.
    /// </summary>
    private void OnScrollWeapon(InputAction.CallbackContext context)
    {
        float scrollValue = context.ReadValue<float>();

        // Ignore tiny scroll values (noise)
        if (Mathf.Abs(scrollValue) < scrollThreshold)
            return;

        // Scroll up (positive value) = next weapon
        if (scrollValue > 0)
        {
            SwitchToNextWeapon();
        }
        // Scroll down (negative value) = previous weapon
        else if (scrollValue < 0)
        {
            SwitchToPreviousWeapon();
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Equips the first unlocked weapon at game start.
    /// If no weapons are unlocked, logs a warning.
    /// </summary>
    private void InitializeStartingWeapon()
    {
        if (availableWeapons == null || availableWeapons.Length == 0)
        {
            Debug.LogError("[WeaponInventory] No weapons assigned! Add weapons to the availableWeapons array.");
            return;
        }

        // Find first unlocked weapon
        int firstUnlockedIndex = -1;
        for (int i = 0; i < availableWeapons.Length; i++)
        {
            if (WeaponUnlockManager.Instance.IsWeaponUnlocked(availableWeapons[i]))
            {
                firstUnlockedIndex = i;
                break;
            }
        }

        if (firstUnlockedIndex != -1)
        {
            SwitchToWeapon(firstUnlockedIndex);
        }
        else
        {
            Debug.LogWarning("[WeaponInventory] No weapons unlocked! Player cannot shoot until a weapon is unlocked.");
        }
    }

    #endregion

    #region Weapon Switching

    /// <summary>
    /// Switches to a specific weapon by index.
    /// Checks if the weapon is unlocked before switching.
    /// </summary>
    /// <param name="weaponIndex">Index in the availableWeapons array</param>
    public void SwitchToWeapon(int weaponIndex)
    {
        // Validate index
        if (weaponIndex < 0 || weaponIndex >= availableWeapons.Length)
        {
            Debug.LogWarning($"[WeaponInventory] Invalid weapon index: {weaponIndex}");
            return;
        }

        WeaponData targetWeapon = availableWeapons[weaponIndex];

        // Check if weapon is unlocked
        if (!WeaponUnlockManager.Instance.IsWeaponUnlocked(targetWeapon))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot switch to {targetWeapon.weaponName} - weapon is LOCKED!");
            return;
        }

        // Switch to the weapon
        currentWeaponIndex = weaponIndex;
        currentWeapon = targetWeapon;

        Debug.Log($"[WeaponInventory] ✅ Switched to: {currentWeapon.weaponName}");

        // ✅ Notify all listeners (UI, shooter, ammo manager, etc.)
        OnWeaponChanged?.Invoke(currentWeapon);
    }

    /// <summary>
    /// Switches to the next unlocked weapon in the loadout (cycles forward).
    /// Useful for Tab key or scroll up.
    /// </summary>
    public void SwitchToNextWeapon()
    {
        if (availableWeapons.Length == 0) return;

        int startIndex = currentWeaponIndex;
        int nextIndex = (currentWeaponIndex + 1) % availableWeapons.Length;

        // Keep cycling until we find an unlocked weapon or loop back to start
        while (nextIndex != startIndex)
        {
            if (WeaponUnlockManager.Instance.IsWeaponUnlocked(availableWeapons[nextIndex]))
            {
                SwitchToWeapon(nextIndex);
                return;
            }
            nextIndex = (nextIndex + 1) % availableWeapons.Length;
        }

        Debug.LogWarning("[WeaponInventory] No other unlocked weapons available!");
    }

    /// <summary>
    /// Switches to the previous unlocked weapon in the loadout (cycles backward).
    /// Useful for scroll down.
    /// </summary>
    public void SwitchToPreviousWeapon()
    {
        if (availableWeapons.Length == 0) return;

        int startIndex = currentWeaponIndex;
        int prevIndex = (currentWeaponIndex - 1 + availableWeapons.Length) % availableWeapons.Length;

        // Keep cycling until we find an unlocked weapon or loop back to start
        while (prevIndex != startIndex)
        {
            if (WeaponUnlockManager.Instance.IsWeaponUnlocked(availableWeapons[prevIndex]))
            {
                SwitchToWeapon(prevIndex);
                return;
            }
            prevIndex = (prevIndex - 1 + availableWeapons.Length) % availableWeapons.Length;
        }

        Debug.LogWarning("[WeaponInventory] No other unlocked weapons available!");
    }

    #endregion

    #region Public Getters

    /// <summary>
    /// Returns the currently equipped weapon.
    /// </summary>
    public WeaponData GetCurrentWeapon()
    {
        return currentWeapon;
    }

    /// <summary>
    /// Returns the name of the currently equipped weapon.
    /// </summary>
    public string GetCurrentWeaponName()
    {
        return currentWeapon?.weaponName ?? "None";
    }

    /// <summary>
    /// Returns the full array of weapons in this inventory.
    /// Useful for UI that shows all weapons (locked/unlocked).
    /// </summary>
    public WeaponData[] GetAllWeapons()
    {
        return availableWeapons;
    }

    /// <summary>
    /// Returns the current weapon's index in the array.
    /// </summary>
    public int GetCurrentWeaponIndex()
    {
        return currentWeaponIndex;
    }

    #endregion
}
