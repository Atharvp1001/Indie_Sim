using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WeaponAmmoManager : MonoBehaviour
{
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

    // ✅ NEW — subscribe to the weapon changed event
    private void OnEnable()
    {
        WeaponInventory.OnWeaponChanged += OnWeaponSwitched;
    }

    // ✅ NEW — always unsubscribe to prevent memory leaks
    private void OnDisable()
    {
        WeaponInventory.OnWeaponChanged -= OnWeaponSwitched;
    }

    private void Start()
    {
        if (playerShooter == null)
        {
            playerShooter = GetComponent<PlayerConeShooter>();
        }

        if (playerShooter != null)
        {
            currentWeapon = playerShooter.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                currentAmmoInMagazine = currentWeapon.magazineCapacity;
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
                currentAmmoInMagazine = currentWeapon.magazineCapacity;
                Debug.Log($"[AmmoManager] Late-initialized weapon: {currentWeapon.weaponName} - Ammo: {currentAmmoInMagazine}");
                UpdateAmmoUI();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            TryReload();
        }

        if (currentAmmoInMagazine <= 0 && !isReloading)
        {
            TryReload();
        }
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
            Debug.Log($"[AmmoManager] Ammo: {currentAmmoInMagazine}/{currentWeapon.magazineCapacity}");
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

        if (currentAmmoInMagazine >= currentWeapon.magazineCapacity)
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

        if (ammoText != null)
            ammoText.text = "...";

        if (audioSource != null && currentWeapon.reloadSound != null)
            audioSource.PlayOneShot(currentWeapon.reloadSound);

        // ✅ NEW — instantly empty the reload icon
        SetReloadFill(0f);

        // ✅ NEW — fill up over reload time
        float elapsed = 0f;
        while (elapsed < currentWeapon.reloadTime)
        {
            elapsed += Time.deltaTime;
            SetReloadFill(Mathf.Clamp01(elapsed / currentWeapon.reloadTime));
            yield return null;
        }

        // ✅ NEW — ensure perfect fill at end
        SetReloadFill(1f);

        currentAmmoInMagazine = currentWeapon.magazineCapacity;
        isReloading = false;

        Debug.Log($"[AmmoManager] Reload complete! Ammo: {currentAmmoInMagazine}/{currentWeapon.magazineCapacity}");

        UpdateAmmoUI();
        reloadCoroutine = null;
    }

    // ✅ NEW
    private void SetReloadFill(float amount)
    {
        if (reloadIcon != null)
            reloadIcon.fillAmount = amount;
    }

    // ✅ Now automatically called by WeaponInventory.OnWeaponChanged event
    public void OnWeaponSwitched(WeaponData newWeapon)
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        isReloading = false;
        currentWeapon = newWeapon;
        currentAmmoInMagazine = currentWeapon.magazineCapacity;

        Debug.Log($"[AmmoManager] Switched to {currentWeapon.weaponName} - Ammo: {currentAmmoInMagazine}/{currentWeapon.magazineCapacity}");

        UpdateAmmoUI();
    }

    public void CancelReload()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
        isReloading = false;
        SetReloadFill(1f); // ✅ NEW — reset icon on cancel
        UpdateAmmoUI();
        Debug.Log("[AmmoManager] Reload cancelled!");
    }


    private void UpdateAmmoUI()
    {
        if (ammoText == null) return;
        int capacity = currentWeapon != null ? currentWeapon.magazineCapacity : 0;
        ammoText.text = $"{currentAmmoInMagazine} / {capacity}";
    }

    public int GetCurrentAmmo() => currentAmmoInMagazine;
    public int GetMagazineCapacity() => currentWeapon != null ? currentWeapon.magazineCapacity : 0;
    public bool IsReloading() => isReloading;
    public float GetReloadProgress() => 0f;
}
