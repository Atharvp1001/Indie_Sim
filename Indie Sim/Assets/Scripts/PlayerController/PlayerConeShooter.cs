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
    [SerializeField] private bool[] weaponUnlockStatus;

    [Header("General Settings")]
    [SerializeField] private LayerMask enemyLayers = -1;
    // CHANGED: Removed coneVisualizer LineRenderer
    // CHANGED: Added Gizmo settings
    [SerializeField] private bool showConeGizmo = true; // Toggle cone gizmo in editor
    [SerializeField] private Color coneGizmoColor = new Color(1f, 0f, 0f, 0.3f); // Red with transparency
    [SerializeField] private float joystickDeadZone = 0.1f;

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

    [Header("Bullet Trail Settings")]
    [SerializeField] private GameObject bulletTrailPrefab;
    [SerializeField] private float bulletTrailSpeed = 50f;
    [SerializeField] private float bulletTrailDuration = 0.2f;
    [SerializeField] private float damageDelay = 0.05f;
    [SerializeField] private int trailPoolSize = 20;

    [Header("Cone Edge Visualizer")]
     private LineRenderer leftEdgeLine;
     private LineRenderer rightEdgeLine;
    [SerializeField] private Color edgeLineColor = Color.yellow;
    [SerializeField] private float edgeLineWidth = 0.05f;

    private List<LineRenderer> trailPool = new List<LineRenderer>();
    private List<LineRenderer> activeTrails = new List<LineRenderer>();

    private bool particlesPlaying = false;
    private WeaponData currentWeapon;

    private float nextFireTime = 0f;
    private bool wasShooting = false;
    private List<IDamageable> damageableTargets = new List<IDamageable>();

    // CHANGED: Store current shooting direction for Gizmo visualization
    private Vector2 currentShootingDirection = Vector2.zero;

    private void Start()
    {
        InitializeWeaponLockSystem();
        InitializeBulletTrailPool();
        //InitializeConeEdgeLines(); // NEW LINE - Add this

        if (availableWeapons.Length > 0)
        {
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


    private void InitializeBulletTrailPool()
    {
        if (bulletTrailPrefab == null)
        {
            Debug.LogWarning("[PlayerConeShooter] No bullet trail prefab assigned! Bullet trails will not appear.");
            return;
        }

        for (int i = 0; i < trailPoolSize; i++)
        {
            GameObject trailObj = Instantiate(bulletTrailPrefab, transform);
            LineRenderer trail = trailObj.GetComponent<LineRenderer>();

            if (trail == null)
            {
                Debug.LogError("[PlayerConeShooter] Bullet trail prefab doesn't have a LineRenderer component!");
                Destroy(trailObj);
                continue;
            }

            trail.positionCount = 2;
            trailObj.SetActive(false);
            trailPool.Add(trail);
        }

        Debug.Log($"[PlayerConeShooter] Bullet trail pool initialized with {trailPool.Count} trails");
    }

    // Create the cone edge line renderers programmatically
   /*
    private void InitializeConeEdgeLines()
    {
        // Create Left Edge Line
        GameObject leftEdgeObj = new GameObject("LeftEdgeLine");
        leftEdgeObj.transform.SetParent(transform);
        leftEdgeObj.transform.localPosition = Vector3.zero;
        leftEdgeLine = leftEdgeObj.AddComponent<LineRenderer>();

        leftEdgeLine.positionCount = 2;
        leftEdgeLine.startWidth = edgeLineWidth;
        leftEdgeLine.endWidth = edgeLineWidth;
        leftEdgeLine.startColor = edgeLineColor;
        leftEdgeLine.endColor = edgeLineColor;
        leftEdgeLine.material = new Material(Shader.Find("Sprites/Default"));

        // CHANGED: Set sorting layer and order
        leftEdgeLine.sortingLayerName = "character"; // Change "Default" to your tilemap's layer if needed
        leftEdgeLine.sortingOrder = 1; // High value to render on top

        leftEdgeLine.useWorldSpace = true;
        leftEdgeLine.enabled = false;

        // Create Right Edge Line
        GameObject rightEdgeObj = new GameObject("RightEdgeLine");
        rightEdgeObj.transform.SetParent(transform);
        rightEdgeObj.transform.localPosition = Vector3.zero;
        rightEdgeLine = rightEdgeObj.AddComponent<LineRenderer>();

        rightEdgeLine.positionCount = 2;
        rightEdgeLine.startWidth = edgeLineWidth;
        rightEdgeLine.endWidth = edgeLineWidth;
        rightEdgeLine.startColor = edgeLineColor;
        rightEdgeLine.endColor = edgeLineColor;
        rightEdgeLine.material = new Material(Shader.Find("Sprites/Default"));

        // CHANGED: Set sorting layer and order
        rightEdgeLine.sortingLayerName = "character"; // Change "Default" to your tilemap's layer if needed
        rightEdgeLine.sortingOrder = 1; // High value to render on top

        rightEdgeLine.useWorldSpace = true;
        rightEdgeLine.enabled = false;

        Debug.Log("[PlayerConeShooter] Cone edge lines created programmatically");
    }
   */


    private LineRenderer GetTrailFromPool()
    {
        foreach (LineRenderer trail in trailPool)
        {
            if (!trail.gameObject.activeInHierarchy)
            {
                trail.gameObject.SetActive(true);
                activeTrails.Add(trail);
                return trail;
            }
        }

        if (activeTrails.Count > 0)
        {
            LineRenderer oldestTrail = activeTrails[0];
            activeTrails.RemoveAt(0);
            activeTrails.Add(oldestTrail);
            return oldestTrail;
        }

        return null;
    }

    private void ReturnTrailToPool(LineRenderer trail)
    {
        if (trail != null)
        {
            trail.gameObject.SetActive(false);
            activeTrails.Remove(trail);
        }
    }

    private void InitializeWeaponLockSystem()
    {
        if (weaponUnlockStatus == null || weaponUnlockStatus.Length != availableWeapons.Length)
        {
            weaponUnlockStatus = new bool[availableWeapons.Length];

            for (int i = 0; i < weaponUnlockStatus.Length; i++)
            {
                weaponUnlockStatus[i] = false;
            }

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

            currentShootingDirection = shootDirection;

            // NEW: Update cone edge visualizer
            UpdateConeEdgeVisual(shootDirection);

            if (Time.time >= nextFireTime)
            {
                FireCone(shootDirection);
                nextFireTime = Time.time + (1f / currentWeapon.fireRate);
            }
            wasShooting = true;
        }
        else
        {
            currentShootingDirection = Vector2.zero;

            // NEW: Hide edge lines when not shooting
            HideConeEdgeVisual();

            if (wasShooting)
            {
                OnStopShooting();
            }
            wasShooting = false;
        }
    }

    // NEW: Update the cone edge lines to show the shooting cone
    private void UpdateConeEdgeVisual(Vector2 direction)
    {
        if (leftEdgeLine == null || rightEdgeLine == null || currentWeapon == null) return;

        // Get the trapezium points
        Vector2[] trapeziumPoints = currentWeapon.GetTrapeziumPoints(firePoint.position, direction);

        // Left edge: from origin to left far corner
        leftEdgeLine.enabled = true;
        leftEdgeLine.SetPosition(0, new Vector3(trapeziumPoints[0].x, trapeziumPoints[0].y, 0));
        leftEdgeLine.SetPosition(1, new Vector3(trapeziumPoints[2].x, trapeziumPoints[2].y, 0));

        // Right edge: from origin to right far corner
        rightEdgeLine.enabled = true;
        rightEdgeLine.SetPosition(0, new Vector3(trapeziumPoints[0].x, trapeziumPoints[0].y, 0));
        rightEdgeLine.SetPosition(1, new Vector3(trapeziumPoints[3].x, trapeziumPoints[3].y, 0));
    }

    // NEW: Hide the cone edge lines
    private void HideConeEdgeVisual()
    {
        if (leftEdgeLine != null)
        {
            leftEdgeLine.enabled = false;
        }

        if (rightEdgeLine != null)
        {
            rightEdgeLine.enabled = false;
        }
    }


    // CHANGED: New Gizmo drawing method - only visible in Unity Editor
    private void OnDrawGizmos()
    {
        if (!showConeGizmo || currentWeapon == null || firePoint == null) return;

        // Only draw if we have a valid shooting direction
        if (currentShootingDirection.magnitude < joystickDeadZone) return;

        // Get trapezium points from weapon
        Vector2[] trapeziumPoints = currentWeapon.GetTrapeziumPoints(firePoint.position, currentShootingDirection);

        // Draw filled trapezium
        Gizmos.color = coneGizmoColor;

        // Draw the cone as a filled polygon
        // Unity Gizmos don't have direct polygon fill, so we draw triangles
        Vector3[] points3D = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            points3D[i] = new Vector3(trapeziumPoints[i].x, trapeziumPoints[i].y, 0);
        }

        // Draw two triangles to fill the trapezium
        DrawGizmoTriangle(points3D[0], points3D[1], points3D[2]);
        DrawGizmoTriangle(points3D[0], points3D[2], points3D[3]);

        // Draw outline
        Gizmos.color = new Color(coneGizmoColor.r, coneGizmoColor.g, coneGizmoColor.b, 1f); // Full opacity for outline
        Gizmos.DrawLine(points3D[0], points3D[1]);
        Gizmos.DrawLine(points3D[1], points3D[2]);
        Gizmos.DrawLine(points3D[2], points3D[3]);
        Gizmos.DrawLine(points3D[3], points3D[0]);
    }

    // CHANGED: Helper method to draw filled triangle
    private void DrawGizmoTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // Draw multiple lines to simulate fill
        int steps = 10;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 start = Vector3.Lerp(p1, p2, t);
            Vector3 end = Vector3.Lerp(p1, p3, t);
            Gizmos.DrawLine(start, end);
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

    public void SwitchToWeapon(int weaponIndex)
    {
        if (weaponIndex >= 0 && weaponIndex < availableWeapons.Length)
        {
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

    public WeaponData[] GetAllWeapons()
    {
        return availableWeapons;
    }

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

    public bool IsWeaponUnlocked(int weaponIndex)
    {
        if (weaponIndex >= 0 && weaponIndex < weaponUnlockStatus.Length)
        {
            return weaponUnlockStatus[weaponIndex];
        }
        return false;
    }

    public int GetUnlockedWeaponCount()
    {
        int count = 0;
        foreach (bool unlocked in weaponUnlockStatus)
        {
            if (unlocked) count++;
        }
        return count;
    }

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

    private void FireCone(Vector2 direction)
    {
        damageableTargets.Clear();
        DetectDamageableTargetsInCone(direction);

        // CHANGED: Always create bullet trails, even if no enemies
        CreateBulletTrailsAndDamage(direction);

        PlayShootEffects();
    }


    private void CreateBulletTrailsAndDamage(Vector2 direction)
    {
        if (currentWeapon.weaponType == WeaponData.WeaponType.Standard)
        {
            // Single bullet - check if we hit an enemy
            IDamageable closestTarget = GetClosestTarget(out Vector3 hitPosition);

            if (closestTarget != null)
            {
                // Hit an enemy - trail goes to enemy and damages it
                StartCoroutine(BulletTrailCoroutine(firePoint.position, hitPosition, closestTarget, currentWeapon.damagePerShot));
            }
            else
            {
                // No enemy - trail goes to max range in shooting direction
                Vector3 endPosition = (Vector3)firePoint.position + new Vector3(direction.x, direction.y, 0) * currentWeapon.coneRange;
                StartCoroutine(BulletTrailCoroutine(firePoint.position, endPosition, null, 0));
            }
        }
        else if (currentWeapon.weaponType == WeaponData.WeaponType.Shotgun)
        {
            // CHANGED: Shotgun fires multiple bullets based on cone angle
            int pelletsPerShot = 5; // Number of shotgun pellets
            float spreadAngle = currentWeapon.GetAngleAtDistance(currentWeapon.coneRange); // Max spread angle

            // Track which enemies we've already hit
            List<IDamageable> hitTargets = new List<IDamageable>();

            for (int i = 0; i < pelletsPerShot; i++)
            {
                // Calculate spread for this pellet
                float angleOffset = Mathf.Lerp(-spreadAngle, spreadAngle, i / (float)(pelletsPerShot - 1));
                Vector2 pelletDirection = RotateVector(direction, angleOffset);

                // Check if this pellet hits an enemy
                IDamageable hitEnemy = GetTargetInDirection(pelletDirection, hitTargets);

                if (hitEnemy != null)
                {
                    // Hit an enemy
                    GameObject targetGO = hitEnemy.GetGameObject();
                    StartCoroutine(BulletTrailCoroutine(firePoint.position, targetGO.transform.position, hitEnemy, currentWeapon.damagePerShot));
                    hitTargets.Add(hitEnemy); // Mark as hit so other pellets can still hit it
                }
                else
                {
                    // No enemy - trail goes to max range
                    Vector3 endPosition = (Vector3)firePoint.position + new Vector3(pelletDirection.x, pelletDirection.y, 0) * currentWeapon.coneRange;
                    StartCoroutine(BulletTrailCoroutine(firePoint.position, endPosition, null, 0));
                }
            }
        }
    }

    private IDamageable GetTargetInDirection(Vector2 direction, List<IDamageable> excludeTargets)
    {
        IDamageable closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (IDamageable target in damageableTargets)
        {
            if (target == null || target.IsDead()) continue;

            GameObject targetGO = target.GetGameObject();
            Vector2 directionToTarget = (targetGO.transform.position - firePoint.position).normalized;
            float distanceToTarget = Vector3.Distance(firePoint.position, targetGO.transform.position);

            // Check if target is in this pellet's direction (within a small angle)
            float angleToTarget = Vector2.Angle(direction, directionToTarget);
            if (angleToTarget > 5f) continue; // 5 degree tolerance per pellet

            // Check for obstacles
            RaycastHit2D hit = Physics2D.Raycast(firePoint.position, directionToTarget, distanceToTarget, obstacleLayers);
            if (hit.collider != null) continue;

            if (distanceToTarget < closestDistance)
            {
                closestDistance = distanceToTarget;
                closestTarget = target;
            }
        }

        return closestTarget;
    }

    private Vector2 RotateVector(Vector2 vector, float angleDegrees)
    {
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angleRadians);
        float sin = Mathf.Sin(angleRadians);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    private IEnumerator BulletTrailCoroutine(Vector3 startPos, Vector3 endPos, IDamageable target, int damage)
    {
        LineRenderer trail = GetTrailFromPool();
        if (trail == null) yield break;

        trail.SetPosition(0, startPos);
        trail.SetPosition(1, startPos);

        float travelTime = Vector3.Distance(startPos, endPos) / bulletTrailSpeed;
        float elapsedTime = 0f;

        // Animate the trail traveling
        while (elapsedTime < travelTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / travelTime;

            Vector3 currentEnd = Vector3.Lerp(startPos, endPos, t);
            trail.SetPosition(1, currentEnd);

            yield return null;
        }

        // Trail reached end position
        trail.SetPosition(1, endPos);

        // CHANGED: Only damage if we actually hit an enemy (target is not null)
        if (target != null)
        {
            yield return new WaitForSeconds(damageDelay);

            if (!target.IsDead())
            {
                target.TakeDamage(damage);

                if (currentWeapon.hitEffect != null)
                {
                    Instantiate(currentWeapon.hitEffect, endPos, Quaternion.identity);
                }

                Debug.Log($"Damaged {target.GetGameObject().name} for {damage} damage with {currentWeapon.weaponName}");
            }
        }

        // Keep trail visible
        yield return new WaitForSeconds(bulletTrailDuration - travelTime - (target != null ? damageDelay : 0));

        // Return trail to pool
        ReturnTrailToPool(trail);
    }

    private IDamageable GetClosestTarget(out Vector3 hitPosition)
    {
        hitPosition = Vector3.zero;

        if (damageableTargets.Count == 0) return null;

        IDamageable closestTarget = null;
        float closestDistance = Mathf.Infinity;

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
                hitPosition = targetGO.transform.position;
            }
        }

        return closestTarget;
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

    // CHANGED: Removed UpdateConeVisual method - no longer needed

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
