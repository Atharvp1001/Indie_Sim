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
            float distanceToTarget = Vector2.Distance(firePoint.position, collider.transform.position);

            // Check if target is within trapezium shape
            if (IsTargetInTrapezium(firePoint.position, direction, distanceToTarget, directionToTarget))
            {
                damageableTargets.Add(damageable);
            }
        }
    }

    private bool IsTargetInTrapezium(Vector2 origin, Vector2 direction, float distance, Vector2 directionToTarget)
    {
        // Use weapon-specific trapezium settings
        float allowedAngle = currentWeapon.GetAngleAtDistance(distance);
        float angleToTarget = Vector2.Angle(direction, directionToTarget);

        return angleToTarget <= allowedAngle;
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

        // Get trapezium points from weapon data
        Vector2[] trapeziumPoints = currentWeapon.GetTrapeziumPoints(firePoint.position, direction);

        // Draw trapezium shape (5 points to close the shape)
        coneVisualizer.positionCount = 5;
        coneVisualizer.SetPosition(0, trapeziumPoints[0]); // Base left
        coneVisualizer.SetPosition(1, trapeziumPoints[3]); // Top left  
        coneVisualizer.SetPosition(2, trapeziumPoints[2]); // Top right
        coneVisualizer.SetPosition(3, trapeziumPoints[1]); // Base right
        coneVisualizer.SetPosition(4, trapeziumPoints[0]); // Close shape
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

        // Draw range circle
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(firePoint.position, currentWeapon.coneRange);

        // Get trapezium points
        Vector2[] points = currentWeapon.GetTrapeziumPoints(firePoint.position, shootDirection);

        // Draw trapezium outline
        Gizmos.color = Color.yellow;
        for (int i = 0; i < 4; i++)
        {
            Vector2 current = points[i];
            Vector2 next = points[(i + 1) % 4];
            Gizmos.DrawLine(current, next);
        }

        // Draw expansion lines at different distances to show the curve
        Gizmos.color = Color.cyan;
        int steps = 5;
        for (int i = 1; i < steps; i++)
        {
            float t = (float)i / steps;
            float distance = Mathf.Lerp(currentWeapon.GetBaseDistance(), currentWeapon.coneRange, t);
            float angle = currentWeapon.GetAngleAtDistance(distance) * Mathf.Deg2Rad;

            Vector2 center = (Vector2)firePoint.position + shootDirection * distance;
            Vector2 perpendicular = new Vector2(-shootDirection.y, shootDirection.x);
            float width = distance * Mathf.Tan(angle);

            Vector2 left = center - perpendicular * width;
            Vector2 right = center + perpendicular * width;
            Gizmos.DrawLine(left, right);
        }
    }


}
