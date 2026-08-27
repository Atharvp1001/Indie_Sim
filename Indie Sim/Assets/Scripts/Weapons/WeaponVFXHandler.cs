using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Handles all visual and audio feedback for weapons.
/// This script "directs the show" - it decides what the player sees and hears.
/// The shooting logic doesn't care about particle effects, lights, or UI - this script does.
/// </summary>
public class WeaponVFXHandler : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Sprite[] weaponSprites; // Weapon icons for UI
    [SerializeField] private Button weaponButton; // The button that shows current weapon

    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem muzzleFlashParticles;
    [SerializeField] private ParticleSystem shellEjectionParticles;
    [SerializeField] private ParticleSystem smokeParticles;

    [Header("Transform References")]
    [SerializeField] private Transform muzzlePoint; // Where muzzle flash spawns
    [SerializeField] private Transform shellEjectionPoint; // Where shells eject from
    [SerializeField] private Transform firePoint; // Where bullets come from (for direction)

    [Header("Muzzle Flash Light")]
    [SerializeField] private Light2D muzzleFlashLight; // 2D light that flashes when shooting
    [SerializeField] private float lightFlashDuration = 0.05f; // How long the light stays on

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Camera Recoil")]
    [SerializeField] private bool enableCameraRecoil = true;

    // Runtime tracking
    private WeaponData currentWeapon;
    private Coroutine currentLightFlashCoroutine = null;
    private PlayerController playerController;

    #region Unity Lifecycle

    private void OnEnable()
    {
        // Subscribe to weapon change events
        WeaponInventory.OnWeaponChanged += HandleWeaponChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        WeaponInventory.OnWeaponChanged -= HandleWeaponChanged;
    }

    private void Start()
    {
        // Initialize muzzle light to off state
        if (muzzleFlashLight != null)
        {
            muzzleFlashLight.enabled = false;
        }

        // Validate particle systems are set to manual emission
        ValidateParticleSystems();

        if (playerController == null) playerController = FindObjectOfType<PlayerController>();

        // Scene-local player (Phase 6) — weaponButton is normally wired
        // directly in the Inspector on the scene-baked RoguelikeMode player
        // instance, but BossArena's player is runtime-spawned from the prefab
        // asset, which carries no scene-specific override. Re-acquire from
        // that scene's own HUD canvas, same pattern as
        // WeaponAmmoManager.ReinitialiseUIReferences(). This object is itself
        // scene-local now (destroyed/recreated per scene load), so a single
        // Start()-time lookup is sufficient — no ongoing subscription needed.
        if (weaponButton == null)
        {
            // Scenes can have more than one Canvas (e.g. BossArena's
            // DemoCompleteCanvas alongside the HUD), so checking only the
            // first Canvas found isn't reliable — search all of them for the
            // one that actually has this button as a child.
            Transform found = null;
            foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                // Two names seen in the HUD prefab for this button across its
                // history — try both rather than guessing wrong and doing nothing.
                found = c.transform.Find("Switch weapon BTN") ?? c.transform.Find("Change Weapon");
                if (found != null) break;
            }

            if (found != null)
            {
                weaponButton = found.GetComponent<Button>();
                if (weaponButton != null)
                    Debug.Log($"[WeaponVFXHandler] Re-acquired weaponButton: {weaponButton.name}");
            }
            else
            {
                Debug.LogWarning("[WeaponVFXHandler] No weapon-switch button found in this scene's HUD — weapon icon UI will not update (cosmetic only).");
            }
        }
    }

    #endregion

    #region Weapon Change Handling

    /// <summary>
    /// Called when the player switches weapons.
    /// Updates the UI to show the new weapon icon.
    /// </summary>
    private void HandleWeaponChanged(WeaponData newWeapon)
    {
        currentWeapon = newWeapon;
        UpdateWeaponUI();
        Debug.Log($"[WeaponVFXHandler] VFX configured for: {newWeapon.weaponName}");
    }

    /// <summary>
    /// Updates the weapon button sprite to match the current weapon.
    /// Maps weapon index to sprite array.
    /// </summary>
    private void UpdateWeaponUI()
    {
        if (weaponButton == null || weaponSprites == null || weaponSprites.Length == 0)
        {
            return;
        }

        // Get the weapon inventory to find current weapon index
        WeaponInventory inventory = FindObjectOfType<WeaponInventory>();
        if (inventory == null) return;

        int weaponIndex = inventory.GetCurrentWeaponIndex();
        int spriteIndex = Mathf.Clamp(weaponIndex, 0, weaponSprites.Length - 1);

        Image buttonImage = weaponButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.sprite = weaponSprites[spriteIndex];
        }
    }

    #endregion

    #region Public VFX Trigger Methods

    /// <summary>
    /// Master method to play all shooting effects.
    /// Call this from PlayerConeShooter when a shot is fired.
    /// </summary>
    /// <param name="shootDirection">Direction the player is shooting (for particle rotation)</param>
    public void PlayShootEffects(Vector2 shootDirection)
    {
        if (currentWeapon == null)
        {
            Debug.LogWarning("[WeaponVFXHandler] Cannot play effects - no weapon equipped!");
            return;
        }

        // Play all visual effects
        PlayMuzzleFlashParticles(shootDirection);
        PlayShellEjectionParticles();
        PlaySmokeParticles(shootDirection);
        FlashMuzzleLight();

        // Play weapon-specific muzzle flash effect (from WeaponData)
        if (currentWeapon.muzzleFlashEffect != null && firePoint != null)
        {
            GameObject flash = Instantiate(currentWeapon.muzzleFlashEffect, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.1f); // Auto-destroy after brief moment
        }

        // Play audio
        PlayShootSound();

        // Apply camera recoil
        ApplyCameraRecoil(shootDirection);
    }

    /// <summary>
    /// Call this when the player stops shooting (mouse released, etc.)
    /// </summary>
    public void OnStopShooting()
    {
        // Stop camera shake/recoil
        if (enableCameraRecoil && CinemachineCursorLead.Instance != null)
        {
            CinemachineCursorLead.Instance.StopFiring();
        }

        // Turn off muzzle light if it's still on
        if (muzzleFlashLight != null)
        {
            muzzleFlashLight.enabled = false;
        }
    }

    #endregion

    #region Particle System Methods

    /// <summary>
    /// Plays muzzle flash particles at the muzzle point, oriented in shoot direction.
    /// </summary>
    private void PlayMuzzleFlashParticles(Vector2 direction)
    {
        if (muzzleFlashParticles == null || muzzlePoint == null) return;

        // Ensure particle system is active
        if (!muzzleFlashParticles.gameObject.activeInHierarchy)
        {
            muzzleFlashParticles.gameObject.SetActive(true);
        }

        // Position at muzzle
        muzzleFlashParticles.transform.position = muzzlePoint.position;

        // Rotate to match shooting direction
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            muzzleFlashParticles.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        // Emit particles (manual emission)
        muzzleFlashParticles.Emit(5);
    }

    /// <summary>
    /// Ejects shell casings from the ejection point.
    /// </summary>
    private void PlayShellEjectionParticles()
    {
        if (shellEjectionParticles == null || shellEjectionPoint == null) return;

        if (!shellEjectionParticles.gameObject.activeInHierarchy)
        {
            shellEjectionParticles.gameObject.SetActive(true);
        }

        // Position at ejection point
        shellEjectionParticles.transform.position = shellEjectionPoint.position;

        // Emit shell casings
        shellEjectionParticles.Emit(2);
    }

    /// <summary>
    /// Plays smoke particles at the muzzle, oriented in shoot direction.
    /// </summary>
    private void PlaySmokeParticles(Vector2 direction)
    {
        if (smokeParticles == null || muzzlePoint == null) return;

        if (!smokeParticles.gameObject.activeInHierarchy)
        {
            smokeParticles.gameObject.SetActive(true);
        }

        // Position at muzzle
        smokeParticles.transform.position = muzzlePoint.position;

        // Rotate to match shooting direction
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            smokeParticles.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        // Emit smoke
        smokeParticles.Emit(3);
    }

    /// <summary>
    /// Validates that all particle systems are set to not auto-play.
    /// This prevents performance issues from continuous emission.
    /// </summary>
    private void ValidateParticleSystems()
    {
        ParticleSystem[] allParticleSystems = { muzzleFlashParticles, shellEjectionParticles, smokeParticles };

        foreach (ParticleSystem ps in allParticleSystems)
        {
            if (ps == null) continue;

            var main = ps.main;
            if (main.playOnAwake)
            {
                Debug.LogWarning($"[WeaponVFXHandler] {ps.name} has 'Play On Awake' enabled. Set to false for better performance!");
            }
        }
    }

    #endregion

    #region Muzzle Flash Light

    /// <summary>
    /// Flashes the 2D light briefly to simulate gun fire illumination.
    /// Uses coroutine to auto-turn-off after duration.
    /// </summary>
    private void FlashMuzzleLight()
    {
        if (muzzleFlashLight == null) return;

        // Stop existing flash if running (prevents stacking)
        if (currentLightFlashCoroutine != null)
        {
            StopCoroutine(currentLightFlashCoroutine);
        }

        // Start new flash
        currentLightFlashCoroutine = StartCoroutine(MuzzleLightFlashCoroutine());
    }

    /// <summary>
    /// Coroutine that handles the light flash timing.
    /// Turns light on, waits, then turns off.
    /// </summary>
    private IEnumerator MuzzleLightFlashCoroutine()
    {
        // Turn light on
        muzzleFlashLight.enabled = true;

        // Wait for the flash duration
        yield return new WaitForSeconds(lightFlashDuration);

        // Turn light off
        muzzleFlashLight.enabled = false;

        // Clear the reference
        currentLightFlashCoroutine = null;
    }

    #endregion

    #region Audio

    /// <summary>
    /// Plays the weapon's shoot sound from the WeaponData scriptable object.
    /// </summary>
    private void PlayShootSound()
    {
        if (audioSource == null) return;
        if (currentWeapon == null || currentWeapon.shootSound == null) return;

        audioSource.PlayOneShot(currentWeapon.shootSound);
    }

    #endregion

    #region Camera Recoil

    /// <summary>
    /// Applies camera shake/recoil in the opposite direction of shooting.
    /// Integrates with CinemachineCursorLead for directional camera effects.
    /// </summary>
    private void ApplyCameraRecoil(Vector2 shootDirection)
    {
        if (!enableCameraRecoil) return;
        if (CinemachineCursorLead.Instance == null) return;

        float strength = currentWeapon != null ? currentWeapon.recoilStrength : 0.5f;
        CinemachineCursorLead.Instance.ApplyRecoil(shootDirection, strength);
        CinemachineCursorLead.Instance.StartFiring();

        if (currentWeapon != null && currentWeapon.playerKnockback > 0f && playerController != null)
        {
            playerController.ApplyKnockback(shootDirection, currentWeapon.playerKnockback);
        }
    }

    #endregion

    #region Public Getters

    /// <summary>
    /// Returns the muzzle point transform (useful for other scripts that need to spawn effects).
    /// </summary>
    public Transform GetMuzzlePoint()
    {
        return muzzlePoint;
    }

    /// <summary>
    /// Returns the fire point transform (where bullets originate).
    /// </summary>
    public Transform GetFirePoint()
    {
        return firePoint;
    }

    #endregion
}
