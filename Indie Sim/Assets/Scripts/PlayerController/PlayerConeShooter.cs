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
    [SerializeField] private WeaponData[] availableWeapons;
    [SerializeField] private int currentWeaponIndex = 0;
    [SerializeField] private KeyCode weaponSwitchKey = KeyCode.Tab;

    [Header("Weapon Lock/Unlock System")]
    [SerializeField] private bool[] weaponUnlockStatus; // Tracks which weapons are unlocked

    [Header("General Settings")]
    [SerializeField] private LayerMask enemyLayers = -1;
    [SerializeField] private bool showConeInEditor = true;
    [SerializeField] private float joystickDeadZone = 0.1f;
    [SerializeField] private LineRenderer coneVisualizer;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("UI References")]
    [SerializeField] private Sprite[] weaponSprites;
    [SerializeField] private Button weaponButton;

    [SerializeField] private LayerMask obstacleLayers;

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem muzzleFlashParticles;
    [SerializeField] private ParticleSystem shellEjectionParticles;
    [SerializeField] private ParticleSystem smokeParticles;

    [Header("Muzzle Point Reference")]
    public Transform muzzlePoint;
    public Transform shellEjectionPoint;

    private bool particlesPlaying = false;
    private WeaponData currentWeapon;

    private float nextFireTime = 0f;
    private bool wasShooting = false;
    private List<IDamageable> damageableTargets = new List<IDamageable>();

    private void Start()
    {
        // Initialize weapon lock system
        InitializeWeaponLockSystem();

        if (availableWeapons.Length > 0)
        {
            // Find the first unlocked weapon
            int firstUnlockedWeapon = -1;
            for (int i = 0; i < weaponUnlockStatus.Length; i++)
            {
                if (weaponUnlockStatus[i])
                {
                    firstUnlockedWeapon = i;
                    break;
                }
            }

            if (firstUnlockedWeapon != -1)
            {
                SwitchToWeapon(firstUnlockedWeapon);
            }
            else
            {
                Debug.LogWarning("[PlayerConeShooter] No weapons unlocked! Unlock at least one weapon to start.");
            }
        }
        else
        {
            Debug.LogError("No weapons assigned to PlayerConeShooter!");
        }
    }

    private void InitializeWeaponLockSystem()
    {
        if (weaponUnlockStatus == null || weaponUnlockStatus.Length != availableWeapons.Length)
        {
            weaponUnlockStatus = new bool[availableWeapons.Length];

            // All weapons start LOCKED
            for (int i = 0; i < weaponUnlockStatus.Length; i++)
            {
                weaponUnlockStatus[i] = false;
            }

            // Pistol (index 0) starts UNLOCKED
            if (weaponUnlockStatus.Length > 0)
            {
                weaponUnlockStatus[0] = true;
            }

            Debug.Log("[PlayerConeShooter] Weapon lock system initialized");
            PrintWeaponLockStatus();
        }
    }

    private void Update()
    {
        HandleWeaponSwitching();
    }

    private void FixedUpdate()
    {
        if (currentWeapon == null) return;

        Vector2 shootDirection = new Vector2(shootingJoystick.Horizontal, shootingJoystick.Vertical);
        bool shouldShoot = shootDirection.magnitude > joystickDeadZone;

        if (shouldShoot)
        {
            shootDirection = shootDirection.normalized;

            if (coneVisualizer != null)
            {
                UpdateConeVisual(shootDirection);
            }

            if (Time.time >= nextFireTime)
            {
                FireCone(shootDirection);
                nextFireTime = Time.time + (1f / currentWeapon.fireRate);
            }
            wasShooting = true;
        }
        else
        {
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
        if (Input.GetKeyDown(weaponSwitchKey))
        {
            SwitchToNextWeapon();
        }

        for (int i = 0; i < availableWeapons.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SwitchToWeapon(i);
            }
        }
    }

    /// <summary>
    /// Switch to a specific weapon by index
    /// Only allowed if the weapon is unlocked
    /// </summary>
    public void SwitchToWeapon(int weaponIndex)
    {
        if (weaponIndex >= 0 && weaponIndex < availableWeapons.Length)
        {
            // Check if weapon is unlocked
            if (!weaponUnlockStatus[weaponIndex])
            {
                Debug.LogWarning($"[PlayerConeShooter] Cannot switch to {availableWeapons[weaponIndex].weaponName} - weapon is LOCKED!");
                return;
            }

            currentWeaponIndex = weaponIndex;
            currentWeapon = availableWeapons[weaponIndex];

            Debug.Log($"[PlayerConeShooter] Switched to: {currentWeapon.weaponName}");

            OnWeaponSwitched();
        }
        else
        {
            Debug.LogWarning($"[PlayerConeShooter] Invalid weapon index: {weaponIndex}");
        }
    }

    /// <summary>
    /// Switch to next unlocked weapon
    /// Skips locked weapons
    /// </summary>
    public void SwitchToNextWeapon()
    {
        int startIndex = currentWeaponIndex;
        int nextIndex = (currentWeaponIndex + 1) % availableWeapons.Length;

        while (nextIndex != startIndex && !weaponUnlockStatus[nextIndex])
        {
            nextIndex = (nextIndex + 1) % availableWeapons.Length;
        }

        if (weaponUnlockStatus[nextIndex])
        {
            SwitchToWeapon(nextIndex);
        }
        else
        {
            Debug.LogWarning("[PlayerConeShooter] No unlocked weapons available!");
        }
    }
    /// <summary>
    /// Get all available weapons array
    /// Used by UpgradeManager to access and modify weapons
    /// </summary>
    public WeaponData[] GetAllWeapons()
    {
        return availableWeapons;
    }

    /// <summary>
    /// Switch to previous unlocked weapon
    /// Skips locked weapons
    /// </summary>
    public void SwitchToPreviousWeapon()
    {
        int startIndex = currentWeaponIndex;
        int prevIndex = (currentWeaponIndex - 1 + availableWeapons.Length) % availableWeapons.Length;

        while (prevIndex != startIndex && !weaponUnlockStatus[prevIndex])
        {
            prevIndex = (prevIndex - 1 + availableWeapons.Length) % availableWeapons.Length;
        }

        if (weaponUnlockStatus[prevIndex])
        {
            SwitchToWeapon(prevIndex);
        }
        else
        {
            Debug.LogWarning("[PlayerConeShooter] No unlocked weapons available!");
        }
    }

    private void OnWeaponSwitched()
    {
        if (weaponButton != null && weaponSprites.Length > 0)
        {
            int spriteIndex = Mathf.Clamp(currentWeaponIndex, 0, weaponSprites.Length - 1);
            Image buttonImage = weaponButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = weaponSprites[spriteIndex];
            }
        }
    }

    // ===== WEAPON UNLOCK SYSTEM =====

    /// <summary>
    /// Unlock the Shotgun weapon
    /// Call this when player finds/earns a shotgun upgrade
    /// </summary>
    public void UnlockShotgun()
    {
        for (int i = 0; i < availableWeapons.Length; i++)
        {
            if (availableWeapons[i].weaponType == WeaponData.WeaponType.Shotgun)
            {
                if (!weaponUnlockStatus[i])
                {
                    weaponUnlockStatus[i] = true;
                    Debug.Log($"[PlayerConeShooter] ✅ SHOTGUN UNLOCKED!");
                    PrintWeaponLockStatus();
                    return;
                }
                else
                {
                    Debug.LogWarning("[PlayerConeShooter] Shotgun is already unlocked!");
                    return;
                }
            }
        }
        Debug.LogError("[PlayerConeShooter] Shotgun not found in available weapons!");
    }

    /// <summary>
    /// Unlock the Machine Gun weapon
    /// Call this when player finds/earns a machine gun upgrade
    /// </summary>
    public void UnlockMachineGun()
    {
        for (int i = 0; i < availableWeapons.Length; i++)
        {
            if (availableWeapons[i].weaponName.ToLower().Contains("machine") ||
                availableWeapons[i].weaponName.ToLower().Contains("ak"))
            {
                if (!weaponUnlockStatus[i])
                {
                    weaponUnlockStatus[i] = true;
                    Debug.Log($"[PlayerConeShooter] ✅ MACHINE GUN UNLOCKED!");
                    PrintWeaponLockStatus();
                    return;
                }
                else
                {
                    Debug.LogWarning("[PlayerConeShooter] Machine Gun is already unlocked!");
                    return;
                }
            }
        }
        Debug.LogError("[PlayerConeShooter] Machine Gun not found in available weapons!");
    }

    /// <summary>
    /// Check if a weapon is unlocked by index
    /// </summary>
    public bool IsWeaponUnlocked(int weaponIndex)
    {
        if (weaponIndex >= 0 && weaponIndex < weaponUnlockStatus.Length)
        {
            return weaponUnlockStatus[weaponIndex];
        }
        return false;
    }

    /// <summary>
    /// Get all unlocked weapons count
    /// </summary>
    public int GetUnlockedWeaponCount()
    {
        int count = 0;
        foreach (bool unlocked in weaponUnlockStatus)
        {
            if (unlocked) count++;
        }
        return count;
    }

    /// <summary>
    /// DEBUG: Print current weapon lock status
    /// </summary>
    public void PrintWeaponLockStatus()
    {
        Debug.Log("========== WEAPON LOCK STATUS ==========");
        for (int i = 0; i < availableWeapons.Length; i++)
        {
            string status = weaponUnlockStatus[i] ? "🔓 UNLOCKED" : "🔒 LOCKED";
            Debug.Log($"Weapon {i}: {availableWeapons[i].weaponName} - {status}");
        }
        Debug.Log($"Total Unlocked: {GetUnlockedWeaponCount()}/{availableWeapons.Length}");
        Debug.Log("=======================================");
    }

    // ===== REST OF SHOOTING CODE (unchanged) =====

    private void FireCone(Vector2 direction)
    {
        damageableTargets.Clear();
        DetectDamageableTargetsInCone(direction);
        DamageAllTargetsInCone(currentWeapon.damagePerShot);
        PlayShootEffects();
    }

    private void DetectDamageableTargetsInCone(Vector2 direction)
    {
        damageableTargets.Clear();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(firePoint.position, currentWeapon.coneRange, enemyLayers);

        foreach (Collider2D collider in colliders)
        {
            IDamageable damageable = collider.GetComponent<IDamageable>();
            if (damageable == null) continue;
            if (damageable.IsDead()) continue;

            Vector2 directionToTarget = (collider.transform.position - firePoint.position).normalized;
            float distanceToTarget = Vector2.Distance(firePoint.position, collider.transform.position);

            if (IsTargetInTrapezium(firePoint.position, direction, distanceToTarget, directionToTarget))
            {
                damageableTargets.Add(damageable);
            }
        }
    }

    private bool IsTargetInTrapezium(Vector2 origin, Vector2 direction, float distance, Vector2 directionToTarget)
    {
        float allowedAngle = currentWeapon.GetAngleAtDistance(distance);
        float angleToTarget = Vector2.Angle(direction, directionToTarget);
        return angleToTarget <= allowedAngle;
    }

    private void DamageAllTargetsInCone(int damageAmount)
    {
        if (currentWeapon.weaponType == WeaponData.WeaponType.Standard)
        {
            DamageClosestTarget(damageAmount);
        }
        else if (currentWeapon.weaponType == WeaponData.WeaponType.Shotgun)
        {
            DamageAllTargets(damageAmount);
        }
    }

    private void DamageClosestTarget(int damageAmount)
    {
        if (damageableTargets.Count == 0) return;

        IDamageable closestTarget = null;
        float closestDistance = Mathf.Infinity;
        GameObject closestTargetGO = null;

        foreach (IDamageable target in damageableTargets)
        {
            if (target == null || target.IsDead()) continue;

            GameObject targetGO = target.GetGameObject();
            float distanceToTarget = Vector3.Distance(firePoint.position, targetGO.transform.position);

            if (distanceToTarget < closestDistance)
            {
                Vector3 directionToTarget = (targetGO.transform.position - firePoint.position).normalized;
                RaycastHit2D hit = Physics2D.Raycast(firePoint.position, directionToTarget, distanceToTarget, obstacleLayers);

                if (hit.collider != null) continue;

                closestDistance = distanceToTarget;
                closestTarget = target;
                closestTargetGO = targetGO;
            }
        }

        if (closestTarget != null && closestTargetGO != null)
        {
            closestTarget.TakeDamage(damageAmount);

            if (currentWeapon.hitEffect != null)
            {
                Instantiate(currentWeapon.hitEffect, closestTargetGO.transform.position, Quaternion.identity);
            }

            Debug.Log($"Damaged {closestTargetGO.name} for {damageAmount} damage with {currentWeapon.weaponName}");
        }
    }

    private void DamageAllTargets(int damageAmount)
    {
        foreach (IDamageable target in damageableTargets)
        {
            if (target != null && !target.IsDead())
            {
                GameObject targetGO = target.GetGameObject();
                Vector3 directionToTarget = (targetGO.transform.position - firePoint.position).normalized;
                float distanceToTarget = Vector3.Distance(firePoint.position, targetGO.transform.position);

                RaycastHit2D hit = Physics2D.Raycast(firePoint.position, directionToTarget, distanceToTarget, obstacleLayers);
                if (hit.collider != null) continue;

                target.TakeDamage(damageAmount);

                if (currentWeapon.hitEffect != null)
                {
                    Instantiate(currentWeapon.hitEffect, targetGO.transform.position, Quaternion.identity);
                }

                Debug.Log($"Damaged {targetGO.name} for {damageAmount} damage with {currentWeapon.weaponName}");
            }
        }
    }

    public void PlayShootEffects()
    {
        CameraShake.Instance.ShakeCamera(1.5f, 0.15f);
        CameraZoomOnSpeed.Instance.StartFiring();

        PlayMuzzleFlashParticles();
        PlayShellEjectionParticles();
        PlaySmokeParticles();

        if (currentWeapon.muzzleFlashEffect != null)
        {
            GameObject flash = Instantiate(currentWeapon.muzzleFlashEffect, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.1f);
        }

        if (audioSource != null && currentWeapon.shootSound != null)
        {
            audioSource.PlayOneShot(currentWeapon.shootSound);
        }
    }

    public void PlayMuzzleFlashParticles()
    {
        if (muzzleFlashParticles != null && muzzlePoint != null)
        {
            if (!muzzleFlashParticles.gameObject.activeInHierarchy)
            {
                muzzleFlashParticles.gameObject.SetActive(true);
            }

            muzzleFlashParticles.transform.position = muzzlePoint.position;
            Vector2 shootDirection = GetShootingDirection();
            if (shootDirection != Vector2.zero)
            {
                float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
                muzzleFlashParticles.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }

            muzzleFlashParticles.Emit(5);
        }
    }

    public void PlayShellEjectionParticles()
    {
        if (shellEjectionParticles != null && shellEjectionPoint != null)
        {
            if (!shellEjectionParticles.gameObject.activeInHierarchy)
            {
                shellEjectionParticles.gameObject.SetActive(true);
            }
            shellEjectionParticles.Emit(2);
        }
    }

    public void PlaySmokeParticles()
    {
        if (smokeParticles != null && muzzlePoint != null)
        {
            if (!smokeParticles.gameObject.activeInHierarchy)
            {
                smokeParticles.gameObject.SetActive(true);
            }

            smokeParticles.transform.position = muzzlePoint.position;
            Vector2 shootDirection = GetShootingDirection();
            if (shootDirection != Vector2.zero)
            {
                float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
                smokeParticles.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }

            smokeParticles.Emit(3);
        }
    }

    public void StopAllParticles()
    {
        particlesPlaying = false;
    }

    private void UpdateConeVisual(Vector2 direction)
    {
        if (coneVisualizer == null || currentWeapon == null) return;
        coneVisualizer.enabled = true;

        Vector2[] trapeziumPoints = currentWeapon.GetTrapeziumPoints(firePoint.position, direction);

        coneVisualizer.positionCount = 5;
        coneVisualizer.SetPosition(0, trapeziumPoints[0]);
        coneVisualizer.SetPosition(1, trapeziumPoints[3]);
        coneVisualizer.SetPosition(2, trapeziumPoints[2]);
        coneVisualizer.SetPosition(3, trapeziumPoints[1]);
        coneVisualizer.SetPosition(4, trapeziumPoints[0]);
    }

    private void OnStopShooting()
    {
        CameraZoomOnSpeed.Instance.StopFiring();
        StopAllParticles();
    }

    public WeaponData GetCurrentWeapon() { return currentWeapon; }
    public string GetCurrentWeaponName() { return currentWeapon?.weaponName ?? "None"; }
    public bool IsShooting() { return wasShooting; }

    public Vector2 GetShootingDirection()
    {
        Vector2 direction = new Vector2(shootingJoystick.Horizontal, shootingJoystick.Vertical);
        return direction.magnitude > joystickDeadZone ? direction.normalized : Vector2.zero;
    }

    public int GetTargetsInCone() { return damageableTargets.Count; }
}
