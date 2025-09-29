using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerConeShooter : MonoBehaviour
{
    [Header("Shooting References")]
    [SerializeField] private FixedJoystick shootingJoystick;
    [SerializeField] private Transform firePoint;

    [Header("Weapon System")]
    [SerializeField] private WeaponData[] availableWeapons; // Array of all weapons
    [SerializeField] private int currentWeaponIndex = 0; // Which weapon is currently equipped
    [SerializeField] private KeyCode weaponSwitchKey = KeyCode.Tab; // For testing weapon switching

    [Header("General Settings")]
    [SerializeField] private LayerMask enemyLayers = -1;
    [SerializeField] private bool showConeInEditor = true;
    [SerializeField] private float joystickDeadZone = 0.1f;
    [SerializeField] private LineRenderer coneVisualizer;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("UI References")]
    [SerializeField] private Sprite[] weaponSprites; // Array to store weapon sprites in order
    [SerializeField] private Button weaponButton; // Reference to the UI button

    [SerializeField] private LayerMask obstacleLayers; // Assign wall layer(s) here in Inspector

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem muzzleFlashParticles;
    [SerializeField] private ParticleSystem shellEjectionParticles;
    [SerializeField] private ParticleSystem smokeParticles;
    private bool particlesPlaying = false;
    // Current weapon properties (gets updated when switching weapons)
    private WeaponData currentWeapon;

    // Private variables
    private float nextFireTime = 0f;
    private bool wasShooting = false;
    private List<IDamageable> damageableTargets = new List<IDamageable>();

    private void Start()
    {
        // Initialize with first weapon
        if (availableWeapons.Length > 0)
        {
            SwitchToWeapon(currentWeaponIndex);
        }
        else
        {
            Debug.LogError("No weapons assigned to PlayerConeShooter!");
        }
    }

    private void Update()
    {
        // Handle weapon switching (for testing - you can integrate this with UI later)
        HandleWeaponSwitching();
    }

    private void FixedUpdate()
    {
        if (currentWeapon == null) return;

        // Get joystick direction
        Vector2 shootDirection = new Vector2(shootingJoystick.Horizontal, shootingJoystick.Vertical);

        // Check if we should shoot
        bool shouldShoot = shootDirection.magnitude > joystickDeadZone;

        if (shouldShoot)
        {
            shootDirection = shootDirection.normalized;

            // Update cone visual if we have one
            if (coneVisualizer != null)
            {
                UpdateConeVisual(shootDirection);
            }

            // Check if it's time to fire
            if (Time.time >= nextFireTime)
            {
                FireCone(shootDirection);
                nextFireTime = Time.time + (1f / currentWeapon.fireRate);
            }
            wasShooting = true;
        }
        else
        {
            // Hide cone visual when not shooting
            if (coneVisualizer != null)
            {
                coneVisualizer.enabled = false;
            }

            if (wasShooting)
            {
                OnStopShooting();
            }
            wasShooting = false;
        }
    }

    private void HandleWeaponSwitching()
    {
        // Simple weapon switching for testing (you can replace this with UI buttons)
        if (Input.GetKeyDown(weaponSwitchKey))
        {
            SwitchToNextWeapon();
        }

        // Number keys for direct weapon selection
        for (int i = 0; i < availableWeapons.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SwitchToWeapon(i);
            }
        }
    }

    public void SwitchToWeapon(int weaponIndex)
    {
        if (weaponIndex >= 0 && weaponIndex < availableWeapons.Length)
        {
            currentWeaponIndex = weaponIndex;
            currentWeapon = availableWeapons[weaponIndex];

            Debug.Log($"Switched to: {currentWeapon.weaponName}");

            // You can add weapon switch effects here
            OnWeaponSwitched();
        }
    }

    public void SwitchToNextWeapon()
    {
        int nextIndex = (currentWeaponIndex + 1) % availableWeapons.Length;
        SwitchToWeapon(nextIndex);
    }

    public void SwitchToPreviousWeapon()
    {
        int prevIndex = (currentWeaponIndex - 1 + availableWeapons.Length) % availableWeapons.Length;
        SwitchToWeapon(prevIndex);
    }

    private void OnWeaponSwitched()
    {
        // Change button sprite to match current weapon
        if (weaponButton != null && weaponSprites.Length > 0)
        {
            // Make sure we don't go out of bounds
            int spriteIndex = Mathf.Clamp(currentWeaponIndex, 0, weaponSprites.Length - 1);

            // Get the Image component from the button and change its sprite
            Image buttonImage = weaponButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = weaponSprites[spriteIndex];
            }
        }
    }


    private void FireCone(Vector2 direction)
    {
        // Clear previous frame's targets
        damageableTargets.Clear();

        // Find all damageable targets in the cone
        DetectDamageableTargetsInCone(direction);

        // Damage all targets in cone
        DamageAllTargetsInCone(currentWeapon.damagePerShot);

        // Visual and audio effects
        PlayShootEffects();
    }

    private void DetectDamageableTargetsInCone(Vector2 direction)
    {
        damageableTargets.Clear();

        // Get all colliders in range using current weapon's range
        Collider2D[] colliders = Physics2D.OverlapCircleAll(firePoint.position, currentWeapon.coneRange, enemyLayers);

        foreach (Collider2D collider in colliders)
        {
            IDamageable damageable = collider.GetComponent<IDamageable>();
            if (damageable == null) continue;

            if (damageable.IsDead()) continue;

            Vector2 directionToTarget = (collider.transform.position - firePoint.position).normalized;
            float angleToTarget = Vector2.Angle(direction, directionToTarget);

            // Use current weapon's cone angle
            if (angleToTarget <= currentWeapon.coneAngle * 0.5f)
            {
                damageableTargets.Add(damageable);
            }
        }
    }

    private void DamageAllTargetsInCone(int damageAmount)
    {
        foreach (IDamageable target in damageableTargets)
        {
            if (target != null && !target.IsDead())
            {
                GameObject targetGO = target.GetGameObject();
                Vector3 directionToTarget = (targetGO.transform.position - firePoint.position).normalized;
                float distanceToTarget = Vector3.Distance(firePoint.position, targetGO.transform.position);

                // Raycast to check obstacles
                RaycastHit2D hit = Physics2D.Raycast(firePoint.position, directionToTarget, distanceToTarget, obstacleLayers);
                if (hit.collider != null)
                {
                    // Obstacle between player and target, skip this target
                    continue;
                }

                // No obstacle, damage target
                target.TakeDamage(damageAmount);

                if (currentWeapon.hitEffect != null)
                {
                    Instantiate(currentWeapon.hitEffect, targetGO.transform.position, Quaternion.identity);
                }

                Debug.Log($"Damaged {targetGO.name} for {damageAmount} damage with {currentWeapon.weaponName}");
            }
        }
    }

    private void PlayShootEffects()
    {
        // When gun fires, add this line:
        CameraShake.Instance.ShakeCamera(1.5f, 0.15f); // intensity, duration
        
        // Camera zoom effect when firing
        CameraZoomOnSpeed.Instance.StartFiring();

        PlayMuzzleFlashParticles();
        PlayShellEjectionParticles();
        PlaySmokeParticles();

        // Use current weapon's muzzle flash
        if (currentWeapon.muzzleFlashEffect != null)
        {
            GameObject flash = Instantiate(currentWeapon.muzzleFlashEffect, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.1f);
        }

        // Use current weapon's shoot sound
        if (audioSource != null && currentWeapon.shootSound != null)
        {
            audioSource.PlayOneShot(currentWeapon.shootSound);
        }
    }


    private void PlayMuzzleFlashParticles()
    {
        if (muzzleFlashParticles != null)
        {
            // Ensure GameObject is active
            if (!muzzleFlashParticles.gameObject.activeInHierarchy)
            {
                muzzleFlashParticles.gameObject.SetActive(true);
            }

            // Orient towards shooting direction
            Vector2 shootDirection = GetShootingDirection();
            if (shootDirection != Vector2.zero)
            {
                float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
                muzzleFlashParticles.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }

            muzzleFlashParticles.Emit(5); // Just emit particles - no Play/Stop issues
        }
    }

    private void PlayShellEjectionParticles()
    {
        if (shellEjectionParticles != null)
        {
            if (!shellEjectionParticles.gameObject.activeInHierarchy)
            {
                shellEjectionParticles.gameObject.SetActive(true);
            }
            shellEjectionParticles.Emit(2);
        }
    }

    private void PlaySmokeParticles()
    {
        if (smokeParticles != null)
        {
            if (!smokeParticles.gameObject.activeInHierarchy)
            {
                smokeParticles.gameObject.SetActive(true);
            }
            smokeParticles.Emit(3);
        }
    }

    // Simplified stop method - no need to stop when using Emit
    private void StopAllParticles()
    {
        // With Emit method, particles naturally fade out
        // No need to actively stop anything
        particlesPlaying = false;
    }





    private void UpdateConeVisual(Vector2 direction)
    {
        if (coneVisualizer == null || currentWeapon == null) return;
        coneVisualizer.enabled = true;

        // Use current weapon's cone angle and range
        float halfAngle = currentWeapon.coneAngle * 0.5f * Mathf.Deg2Rad;

        Vector3 centerDirection = new Vector3(direction.x, direction.y, 0) * currentWeapon.coneRange;
        Vector3 leftEdge = Quaternion.Euler(0, 0, currentWeapon.coneAngle * 0.5f) * centerDirection;
        Vector3 rightEdge = Quaternion.Euler(0, 0, -currentWeapon.coneAngle * 0.5f) * centerDirection;

        coneVisualizer.positionCount = 4;
        coneVisualizer.SetPosition(0, firePoint.position);
        coneVisualizer.SetPosition(1, firePoint.position + leftEdge);
        coneVisualizer.SetPosition(2, firePoint.position + rightEdge);
        coneVisualizer.SetPosition(3, firePoint.position);
    }

    private void OnStopShooting()
    {
        // Reset camera zoom when stopping shooting
        CameraZoomOnSpeed.Instance.StopFiring();

        StopAllParticles();
    }


   



    // Public methods for getting current weapon info
    public WeaponData GetCurrentWeapon() { return currentWeapon; }
    public string GetCurrentWeaponName() { return currentWeapon?.weaponName ?? "None"; }
    public bool IsShooting() { return wasShooting; }

    public Vector2 GetShootingDirection()
    {
        Vector2 direction = new Vector2(shootingJoystick.Horizontal, shootingJoystick.Vertical);
        return direction.magnitude > joystickDeadZone ? direction.normalized : Vector2.zero;
    }

    public int GetTargetsInCone() { return damageableTargets.Count; }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (!showConeInEditor || firePoint == null || currentWeapon == null) return;

        Vector2 shootDirection = GetShootingDirection();
        if (shootDirection == Vector2.zero)
        {
            shootDirection = Vector2.right;
        }

        // Draw cone range using current weapon's range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(firePoint.position, currentWeapon.coneRange);

        // Draw cone edges using current weapon's angle
        float halfAngle = currentWeapon.coneAngle * 0.5f;
        Vector3 centerDirection = new Vector3(shootDirection.x, shootDirection.y, 0) * currentWeapon.coneRange;
        Vector3 leftEdge = Quaternion.Euler(0, 0, halfAngle) * centerDirection;
        Vector3 rightEdge = Quaternion.Euler(0, 0, -halfAngle) * centerDirection;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(firePoint.position, firePoint.position + leftEdge);
        Gizmos.DrawLine(firePoint.position, firePoint.position + rightEdge);
        Gizmos.DrawLine(firePoint.position + leftEdge, firePoint.position + rightEdge);
    }
}
