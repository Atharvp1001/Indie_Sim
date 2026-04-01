using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponAmmoManager : MonoBehaviour
{
    // ✅ Bug 1 Fix: Add singleton
    public static WeaponAmmoManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerConeShooter playerShooter;
    [SerializeField] private AudioSource audioSource;

    [Header("Ammo UI")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Image reloadIcon;

    [Header("Debug Info")]
    [SerializeField] private int currentAmmoInMagazine;
    [SerializeField] private bool isReloading = false;

    private WeaponData currentWeapon;
    private Coroutine reloadCoroutine;

    // Tracks per-weapon ammo so switching weapons doesn't reset ammo
    private Dictionary<WeaponData, int> _currentAmmo = new Dictionary<WeaponData, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        WeaponInventory.OnWeaponChanged += OnWeaponSwitched;
    }

    private void OnDisable()
    {
        WeaponInventory.OnWeaponChanged -= OnWeaponSwitched;
    }

    private void Start()
    {
        if (playerShooter == null)
            playerShooter = GetComponent<PlayerConeShooter>();

        if (playerShooter != null)
        {
            currentWeapon = playerShooter.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                // Only fill to max if we haven't saved ammo for this weapon yet
                if (!_currentAmmo.ContainsKey(currentWeapon))
                    _currentAmmo[currentWeapon] = GetCurrentMaxAmmo();

                currentAmmoInMagazine = _currentAmmo[currentWeapon];
                UpdateAmmoUI();
            }
        }
    }

    private void Update()
    {
        if (currentWeapon == null && playerShooter != null)
        {
            currentWeapon = playerShooter.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                if (!_currentAmmo.ContainsKey(currentWeapon))
                    _currentAmmo[currentWeapon] = GetCurrentMaxAmmo();

                currentAmmoInMagazine = _currentAmmo[currentWeapon];
                Debug.Log($"[AmmoManager] Late-initialized weapon: {currentWeapon.weaponName} - Ammo: {currentAmmoInMagazine}");
                UpdateAmmoUI();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
            TryReload();

        if (currentAmmoInMagazine <= 0 && !isReloading)
            TryReload();
    }

    public bool CanShoot()
    {
        return currentAmmoInMagazine > 0 && !isReloading;
    }

    public void ConsumeBullet()
    {
        if (currentAmmoInMagazine > 0)
        {
            currentAmmoInMagazine--;

            // ✅ Keep dictionary in sync so weapon switch restores correct count
            if (currentWeapon != null)
                _currentAmmo[currentWeapon] = currentAmmoInMagazine;

            Debug.Log($"[AmmoManager] Ammo: {currentAmmoInMagazine}/{GetCurrentMaxAmmo()}");
            UpdateAmmoUI();
        }
    }

    public void TryReload()
    {
        if (currentWeapon == null)
        {
            Debug.LogWarning("[AmmoManager] TryReload called but currentWeapon is null!");
            return;
        }

        if (isReloading)
        {
            Debug.Log("[AmmoManager] Already reloading!");
            return;
        }

        if (currentAmmoInMagazine >= GetCurrentMaxAmmo())
        {
            Debug.Log("[AmmoManager] Magazine already full!");
            return;
        }

        if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
        reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        Debug.Log($"[AmmoManager] Reloading {currentWeapon.weaponName}...");

        if (ammoText != null) ammoText.text = "...";

        if (audioSource != null && currentWeapon.reloadSound != null)
            audioSource.PlayOneShot(currentWeapon.reloadSound);

        SetReloadFill(0f);

        float elapsed = 0f;
        while (elapsed < currentWeapon.reloadTime)
        {
            elapsed += Time.deltaTime;
            SetReloadFill(Mathf.Clamp01(elapsed / currentWeapon.reloadTime));
            yield return null;
        }

        SetReloadFill(1f);

        int maxAmmo = GetCurrentMaxAmmo();
        currentAmmoInMagazine = maxAmmo;

        // ✅ Keep dictionary in sync
        if (currentWeapon != null)
            _currentAmmo[currentWeapon] = currentAmmoInMagazine;

        isReloading = false;
        Debug.Log($"[AmmoManager] Reload complete! Ammo: {currentAmmoInMagazine}/{maxAmmo}");
        UpdateAmmoUI();
        reloadCoroutine = null;
    }

    private void SetReloadFill(float amount)
    {
        if (reloadIcon != null) reloadIcon.fillAmount = amount;
    }

    public void OnWeaponSwitched(WeaponData newWeapon)
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        // ✅ Bug 3 Fix: Save ammo for old weapon before switching
        if (currentWeapon != null)
            _currentAmmo[currentWeapon] = currentAmmoInMagazine;

        isReloading = false;
        currentWeapon = newWeapon;

        // ✅ Restore saved ammo for new weapon, or fill to max if first time
        if (newWeapon != null)
        {
            if (!_currentAmmo.ContainsKey(newWeapon))
                _currentAmmo[newWeapon] = GetCurrentMaxAmmo();

            currentAmmoInMagazine = _currentAmmo[newWeapon];
        }

        Debug.Log($"[AmmoManager] Switched to {currentWeapon?.weaponName} - Ammo: {currentAmmoInMagazine}/{GetCurrentMaxAmmo()}");
        UpdateAmmoUI();
    }

    // ✅ Bug 2 Fix: Add RefillAmmo — called by UpgradeManager after an ammo upgrade
    /// <summary>
    /// Called by UpgradeManager when an ammo upgrade is applied.
    /// Tops up the upgraded weapon to its new max capacity.
    /// </summary>
    public void RefillAmmo(WeaponData weapon)
    {
        if (weapon == null) return;

        int newMax = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetFinalAmmo(weapon)
            : weapon.magazineCapacity;

        _currentAmmo[weapon] = newMax;

        // If the upgraded weapon is currently equipped, update the live counter too
        if (currentWeapon == weapon)
        {
            currentAmmoInMagazine = newMax;
            UpdateAmmoUI();
        }

        Debug.Log($"[AmmoManager] Refilled {weapon.weaponName} to {newMax} after upgrade.");
    }

    public void CancelReload()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
        isReloading = false;
        SetReloadFill(1f);
        UpdateAmmoUI();
        Debug.Log("[AmmoManager] Reload cancelled!");
    }

    private void UpdateAmmoUI()
    {
        if (ammoText == null) return;
        ammoText.text = $"{currentAmmoInMagazine} / {GetCurrentMaxAmmo()}";
    }

    public void InitialiseAmmo(WeaponData[] weapons)
    {
        _currentAmmo.Clear();
        foreach (WeaponData weapon in weapons)
        {
            if (weapon == null) continue;
            int max = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetFinalAmmo(weapon)
                : weapon.magazineCapacity;
            _currentAmmo[weapon] = max;
        }
    }

    private int GetCurrentMaxAmmo()
    {
        if (currentWeapon == null) return 0;
        if (UpgradeManager.Instance != null)
            return UpgradeManager.Instance.GetFinalAmmo(currentWeapon);
        return currentWeapon.magazineCapacity;
    }

    public int GetCurrentAmmo() => currentAmmoInMagazine;
    public int GetMagazineCapacity() => GetCurrentMaxAmmo();
    public bool IsReloading() => isReloading;
    public float GetReloadProgress() => 0f;
}