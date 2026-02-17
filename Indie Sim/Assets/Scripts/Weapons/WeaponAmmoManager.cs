using System.Collections;
using UnityEngine;

public class WeaponAmmoManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerConeShooter playerShooter; // Reference to your shooting script
    [SerializeField] private AudioSource audioSource; // For reload sound

    [Header("Debug Info")]
    [SerializeField] private int currentAmmoInMagazine; // Current bullets in magazine
    [SerializeField] private bool isReloading = false; // Is currently reloading?

    private WeaponData currentWeapon;
    private Coroutine reloadCoroutine;

    private void Start()
    {
        // Get shooting script reference if not assigned
        if (playerShooter == null)
        {
            playerShooter = GetComponent<PlayerConeShooter>();
        }

        // Initialize ammo for starting weapon
        if (playerShooter != null)
        {
            currentWeapon = playerShooter.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                currentAmmoInMagazine = currentWeapon.magazineCapacity;
            }
        }
    }

    private void Update()
    {
        // Listen for reload input (R key)
        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            TryReload();
        }

        // Auto-reload if magazine is empty and not already reloading
        if (currentAmmoInMagazine <= 0 && !isReloading)
        {
            TryReload();
        }
    }

    /// <summary>
    /// Check if player can shoot (has ammo and not reloading)
    /// Call this from your shooting script before firing
    /// </summary>
    public bool CanShoot()
    {
        return currentAmmoInMagazine > 0 && !isReloading;
    }

    /// <summary>
    /// Consume one bullet from magazine
    /// Call this from your shooting script when you fire
    /// </summary>
    public void ConsumeBullet()
    {
        if (currentAmmoInMagazine > 0)
        {
            currentAmmoInMagazine--;
            Debug.Log($"[AmmoManager] Ammo: {currentAmmoInMagazine}/{currentWeapon.magazineCapacity}");
        }
    }

    /// <summary>
    /// Attempt to reload the weapon
    /// </summary>
    public void TryReload()
    {
        // Don't reload if already reloading
        if (isReloading)
        {
            Debug.Log("[AmmoManager] Already reloading!");
            return;
        }

        // Don't reload if magazine is already full
        if (currentAmmoInMagazine >= currentWeapon.magazineCapacity)
        {
            Debug.Log("[AmmoManager] Magazine already full!");
            return;
        }

        // Start reload coroutine
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
        }
        reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    /// <summary>
    /// Handles the reload process
    /// </summary>
    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        Debug.Log($"[AmmoManager] Reloading {currentWeapon.weaponName}...");

        // Play reload sound
        if (audioSource != null && currentWeapon.reloadSound != null)
        {
            audioSource.PlayOneShot(currentWeapon.reloadSound);
        }

        // Wait for reload time
        yield return new WaitForSeconds(currentWeapon.reloadTime);

        // Refill magazine (infinite reserve for now)
        currentAmmoInMagazine = currentWeapon.magazineCapacity;
        isReloading = false;

        Debug.Log($"[AmmoManager] Reload complete! Ammo: {currentAmmoInMagazine}/{currentWeapon.magazineCapacity}");

        reloadCoroutine = null;
    }

    /// <summary>
    /// Called when player switches weapons
    /// Resets ammo to full magazine for new weapon
    /// </summary>
    public void OnWeaponSwitched(WeaponData newWeapon)
    {
        // Stop any active reload
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        isReloading = false;
        currentWeapon = newWeapon;

        // Start with full magazine
        currentAmmoInMagazine = currentWeapon.magazineCapacity;

        Debug.Log($"[AmmoManager] Switched to {currentWeapon.weaponName} - Ammo: {currentAmmoInMagazine}/{currentWeapon.magazineCapacity}");
    }

    /// <summary>
    /// Force cancel reload (e.g., if player gets stunned)
    /// </summary>
    public void CancelReload()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
        isReloading = false;
        Debug.Log("[AmmoManager] Reload cancelled!");
    }

    // Getters for UI or other systems
    public int GetCurrentAmmo() => currentAmmoInMagazine;
    public int GetMagazineCapacity() => currentWeapon != null ? currentWeapon.magazineCapacity : 0;
    public bool IsReloading() => isReloading;
    public float GetReloadProgress()
    {
        // You can implement this if you want a reload progress bar
        return 0f;
    }
}
