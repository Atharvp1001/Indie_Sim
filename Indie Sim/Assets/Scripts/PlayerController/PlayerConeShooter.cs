using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;


public class PlayerConeShooter : MonoBehaviour
{
    [Header("Shooting References")]
    // REMOVED: [SerializeField] private FixedJoystick shootingJoystick;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera mainCamera; // Needed to convert mouse to world position

    [Header("Weapon System")]
    [SerializeField] private WeaponData[] availableWeapons;
    [SerializeField] private int currentWeaponIndex = 0;
  

    [Header("Weapon Lock/Unlock System")]
    [SerializeField] private bool[] weaponUnlockStatus;

    [Header("General Settings")]
    [SerializeField] private LayerMask enemyLayers = -1;
    [SerializeField] private bool showConeGizmo = true;
    [SerializeField] private Color coneGizmoColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private float joystickDeadZone = 0.1f; // Can rename to "aimDeadZone" if preferred

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
    [SerializeField] private float coneFlashDuration = 0.1f;
    private bool isShowingConeFlash = false;

    [Header("Muzzle Flash Light")]
    [SerializeField] private Light2D muzzleFlashLight; // The 2D light on player
    [SerializeField] private float lightFlashDuration = 0.05f; // How long light stays on
    private Coroutine currentLightFlash = null;

    [Header("Bullet Trail Collision Detection")]
    [SerializeField] private LayerMask bulletTrailWallLayers; // Walls/obstacles that stop bullets
    [SerializeField] private LayerMask bulletTrailEnemyLayers; // Enemies that bullets can hit


    private List<LineRenderer> trailPool = new List<LineRenderer>();
    private List<LineRenderer> activeTrails = new List<LineRenderer>();

    private bool particlesPlaying = false;
    private WeaponData currentWeapon;

    private float nextFireTime = 0f;
    private bool wasShooting = false;
    private List<IDamageable> damageableTargets = new List<IDamageable>();

    private Vector2 currentShootingDirection = Vector2.zero;

    // Input System Variables
    private PlayerControls inputActions;
    private Vector2 mousePositionInput;
    private bool isFiring = false;

    private void Awake()
    {
        inputActions = new PlayerControls();

        // Bind Look (Mouse Position)
        //inputActions.Player.Look.performed += ctx => mousePositionInput = ctx.ReadValue<Vector2>();

        inputActions.Player.Fire.performed += ctx =>
        {
            isFiring = true;
            //Debug.Log("[PlayerConeShooter] ✅ FIRE STARTED via Input System");
        };

        inputActions.Player.Fire.canceled += ctx =>
        {
            isFiring = false;
            //Debug.Log("[PlayerConeShooter] ❌ FIRE STOPPED via Input System");
        };

        inputActions.Player.SwitchWeapon.performed += ctx => SwitchToNextWeapon(); // weapon switching


        if (mainCamera == null) mainCamera = Camera.main;

        // Debug.Log("[PlayerConeShooter] Input Actions initialized");
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
        InitializeWeaponLockSystem();
        InitializeBulletTrailPool();
        InitializeConeEdgeLines();

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
                //Debug.LogWarning("[PlayerConeShooter] No weapons unlocked! Unlock at least one weapon to start.");
            }
        }
        else
        {
            //Debug.LogError("No weapons assigned to PlayerConeShooter!");
        }

        if (muzzleFlashLight != null)
        {
            muzzleFlashLight.enabled = false;
        }
    }

    private void InitializeBulletTrailPool()
    {
        if (bulletTrailPrefab == null)
        {
            //Debug.LogWarning("[PlayerConeShooter] No bullet trail prefab assigned! Bullet trails will not appear.");
            return;
        }

        for (int i = 0; i < trailPoolSize; i++)
        {
            GameObject trailObj = Instantiate(bulletTrailPrefab, transform);
            LineRenderer trail = trailObj.GetComponent<LineRenderer>();

            if (trail == null)
            {
                // Debug.LogError("[PlayerConeShooter] Bullet trail prefab doesn't have a LineRenderer component!");
                Destroy(trailObj);
                continue;
            }

            trail.positionCount = 2;
            trailObj.SetActive(false);
            trailPool.Add(trail);
        }

        //Debug.Log($"[PlayerConeShooter] Bullet trail pool initialized with {trailPool.Count} trails");
    }

    private void InitializeConeEdgeLines()
    {
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
        leftEdgeLine.sortingLayerName = "character";
        leftEdgeLine.sortingOrder = 1;
        leftEdgeLine.useWorldSpace = true;
        leftEdgeLine.enabled = false;

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
        rightEdgeLine.sortingLayerName = "character";
        rightEdgeLine.sortingOrder = 1;
        rightEdgeLine.useWorldSpace = true;
        rightEdgeLine.enabled = false;

        //Debug.Log("[PlayerConeShooter] Cone edge lines created programmatically");
    }

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
        

        if (Input.GetMouseButton(0))
        {
            // Debug.LogError("🔥🔥🔥 LEFT MOUSE CLICKED!"); // Using LogError so it shows in red
            isFiring = true;
        }
        else
        {
            isFiring = false;
        }

        HandleWeaponSwitching();
    }

    private void FixedUpdate()
    {
        if (currentWeapon == null) return;

        // Calculate shooting direction from player to mouse
        Vector2 shootDirection = GetMouseAimDirection();
        bool shouldShoot = isFiring && shootDirection.magnitude > 0.01f;

        if (shouldShoot)
        {
            currentShootingDirection = shootDirection;

            if (Time.time >= nextFireTime)
            {
                FireCone(shootDirection);
                ShowConeFlash(shootDirection);
                nextFireTime = Time.time + (1f / currentWeapon.fireRate);
            }
            wasShooting = true;
        }
        else
        {
            currentShootingDirection = Vector2.zero;

            if (!isShowingConeFlash)
            {
                HideConeEdgeVisual();
            }

            if (wasShooting)
            {
                OnStopShooting();
            }
            wasShooting = false;
        }
    }

    /// <summary>
    /// Calculate direction from player/firepoint to mouse cursor
    /// </summary>
    private Vector2 GetMouseAimDirection()
    {
        if (mainCamera == null || firePoint == null)
        {
            //Debug.LogWarning("[GetMouseAimDirection] Missing camera or firePoint!");
            return Vector2.zero;
        }

        // ✅ Use Input.mousePosition directly (works with both Input systems)
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z - firePoint.position.z);

        Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        Vector2 direction = ((Vector2)worldMousePos - (Vector2)firePoint.position).normalized;

        return direction;
    }


    private void ShowConeFlash(Vector2 direction)
    {
        if (isShowingConeFlash) return;
        StartCoroutine(ConeFlashCoroutine(direction));
    }

    private IEnumerator ConeFlashCoroutine(Vector2 direction)
    {
        isShowingConeFlash = true;
        UpdateConeEdgeVisual(direction);
        yield return new WaitForSeconds(coneFlashDuration);
        HideConeEdgeVisual();
        isShowingConeFlash = false;
    }

    private void UpdateConeEdgeVisual(Vector2 direction)
    {
        if (leftEdgeLine == null || rightEdgeLine == null || currentWeapon == null) return;

        Vector2[] trapeziumPoints = currentWeapon.GetTrapeziumPoints(firePoint.position, direction);

        leftEdgeLine.enabled = true;
        leftEdgeLine.SetPosition(0, new Vector3(trapeziumPoints[0].x, trapeziumPoints[0].y, 0));
        leftEdgeLine.SetPosition(1, new Vector3(trapeziumPoints[3].x, trapeziumPoints[3].y, 0));

        rightEdgeLine.enabled = true;
        rightEdgeLine.SetPosition(0, new Vector3(trapeziumPoints[1].x, trapeziumPoints[1].y, 0));
        rightEdgeLine.SetPosition(1, new Vector3(trapeziumPoints[2].x, trapeziumPoints[2].y, 0));
    }

    private void HideConeEdgeVisual()
    {
        if (leftEdgeLine != null) leftEdgeLine.enabled = false;
        if (rightEdgeLine != null) rightEdgeLine.enabled = false;
    }

    private void OnDrawGizmos()
    {
        if (!showConeGizmo || currentWeapon == null || firePoint == null) return;
        if (currentShootingDirection.magnitude < joystickDeadZone) return;

        Vector2[] trapeziumPoints = currentWeapon.GetTrapeziumPoints(firePoint.position, currentShootingDirection);

        Gizmos.color = coneGizmoColor;

        Vector3[] points3D = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            points3D[i] = new Vector3(trapeziumPoints[i].x, trapeziumPoints[i].y, 0);
        }

        DrawGizmoTriangle(points3D[0], points3D[1], points3D[2]);
        DrawGizmoTriangle(points3D[0], points3D[2], points3D[3]);

        Gizmos.color = new Color(coneGizmoColor.r, coneGizmoColor.g, coneGizmoColor.b, 1f);
        Gizmos.DrawLine(points3D[0], points3D[1]);
        Gizmos.DrawLine(points3D[1], points3D[2]);
        Gizmos.DrawLine(points3D[2], points3D[3]);
        Gizmos.DrawLine(points3D[3], points3D[0]);
    }

    private void DrawGizmoTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
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
        CreateBulletTrailsAndDamage(direction);
        PlayShootEffects();
    }

    private void CreateBulletTrailsAndDamage(Vector2 direction)
    {
        if (currentWeapon.weaponType == WeaponData.WeaponType.Standard)
        {
            IDamageable closestTarget = GetClosestTarget(out Vector3 hitPosition);

            if (closestTarget != null)
            {
                // Hit a target - check if there's a wall between player and target
                Vector3 directionToTarget = (hitPosition - firePoint.position).normalized;
                float distanceToTarget = Vector3.Distance(firePoint.position, hitPosition);

                RaycastHit2D wallCheck = Physics2D.Raycast(firePoint.position, directionToTarget, distanceToTarget, bulletTrailWallLayers);

                if (wallCheck.collider != null)
                {
                    // Wall blocks the shot - trail stops at wall, no damage
                    // Add random spread since we didn't hit the target
                    Vector2 randomDirection = AddRandomSpread(direction, 5f);
                    Vector3 randomEndPos = GetTrailEndPosition(firePoint.position, randomDirection, currentWeapon.coneRange);
                    StartCoroutine(BulletTrailCoroutine(firePoint.position, randomEndPos, null, 0, Vector3.zero));
                }
                else
                {
                    // Clear shot - trail stops at enemy (NO random spread on hits)
                    StartCoroutine(BulletTrailCoroutine(firePoint.position, hitPosition, closestTarget, currentWeapon.damagePerShot, hitPosition));
                }
            }
            else
            {
                // No enemy hit - ADD RANDOM SPREAD
                Vector2 randomDirection = AddRandomSpread(direction, 5f);
                Vector3 maxRangePosition = GetTrailEndPosition(firePoint.position, randomDirection, currentWeapon.coneRange);
                StartCoroutine(BulletTrailCoroutine(firePoint.position, maxRangePosition, null, 0, Vector3.zero));
            }
        }
        else if (currentWeapon.weaponType == WeaponData.WeaponType.Shotgun)
        {
            int pelletsPerShot = 5;
            float spreadAngle = currentWeapon.GetAngleAtDistance(currentWeapon.coneRange);

            List<IDamageable> hitTargets = new List<IDamageable>();

            for (int i = 0; i < pelletsPerShot; i++)
            {
                float angleOffset = Mathf.Lerp(-spreadAngle, spreadAngle, i / (float)(pelletsPerShot - 1));
                Vector2 pelletDirection = RotateVector(direction, angleOffset);

                IDamageable hitEnemy = GetTargetInDirection(pelletDirection, hitTargets);

                if (hitEnemy != null)
                {
                    GameObject targetGO = hitEnemy.GetGameObject();
                    Vector3 hitPos = targetGO.transform.position;
                    float distanceToEnemy = Vector3.Distance(firePoint.position, hitPos);

                    // Check for walls between player and enemy
                    RaycastHit2D wallCheck = Physics2D.Raycast(firePoint.position, pelletDirection, distanceToEnemy, bulletTrailWallLayers);

                    if (wallCheck.collider != null)
                    {
                        // Wall blocks this pellet - ADD RANDOM SPREAD
                        Vector2 randomDirection = AddRandomSpread(pelletDirection, 5f);
                        Vector3 randomEndPos = GetTrailEndPosition(firePoint.position, randomDirection, currentWeapon.coneRange);
                        StartCoroutine(BulletTrailCoroutine(firePoint.position, randomEndPos, null, 0, Vector3.zero));
                    }
                    else
                    {
                        // Clear shot - trail stops at enemy (NO random spread on hits)
                        StartCoroutine(BulletTrailCoroutine(firePoint.position, hitPos, hitEnemy, currentWeapon.damagePerShot, hitPos));
                        hitTargets.Add(hitEnemy);
                    }
                }
                else
                {
                    // No enemy hit - ADD RANDOM SPREAD
                    Vector2 randomDirection = AddRandomSpread(pelletDirection, 5f);
                    Vector3 maxRangePosition = GetTrailEndPosition(firePoint.position, randomDirection, currentWeapon.coneRange);
                    StartCoroutine(BulletTrailCoroutine(firePoint.position, maxRangePosition, null, 0, Vector3.zero));
                }
            }
        }
    }

    /// <summary>
    /// Adds random spread to a direction vector
    /// </summary>
    /// <param name="direction">Original direction</param>
    /// <param name="maxSpreadDegrees">Maximum random spread in degrees (±)</param>
    /// <returns>New direction with random spread applied</returns>
    private Vector2 AddRandomSpread(Vector2 direction, float maxSpreadDegrees)
    {
        // Generate random angle between -maxSpreadDegrees and +maxSpreadDegrees
        float randomAngle = Random.Range(-maxSpreadDegrees, maxSpreadDegrees);

        // Rotate the direction by the random angle
        return RotateVector(direction, randomAngle);
    }


    /// <summary>
    /// Calculates where the bullet trail should end, checking for walls
    /// Returns either the wall hit position or max range position
    /// </summary>
    private Vector3 GetTrailEndPosition(Vector3 startPos, Vector2 direction, float maxRange)
    {
        // First check for walls
        RaycastHit2D wallHit = Physics2D.Raycast(startPos, direction, maxRange, bulletTrailWallLayers);

        if (wallHit.collider != null)
        {
            // Hit a wall - trail stops at wall
            return wallHit.point;
        }

        // No wall hit - trail travels full range
        return startPos + new Vector3(direction.x, direction.y, 0) * maxRange;
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

            float angleToTarget = Vector2.Angle(direction, directionToTarget);
            if (angleToTarget > 5f) continue;

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

    private IEnumerator BulletTrailCoroutine(Vector3 startPos, Vector3 endPos, IDamageable target, int damage, Vector3 hitPosition)
    {
        // Get a trail from the pool
        LineRenderer trail = GetTrailFromPool();

        if (trail == null)
        {
            Debug.LogWarning("[PlayerConeShooter] No available trails in pool!");
            yield break;
        }

        // Set up the trail positions
        trail.SetPosition(0, startPos);
        trail.SetPosition(1, startPos); // Start both points at origin

        float distance = Vector3.Distance(startPos, endPos);
        float travelTime = distance / bulletTrailSpeed;
        float elapsedTime = 0f;

        bool damageApplied = false;

        // Animate the trail from start to end
        while (elapsedTime < travelTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / travelTime;

            // Keep start point at origin, move end point toward final position
            trail.SetPosition(0, startPos);
            trail.SetPosition(1, Vector3.Lerp(startPos, endPos, t));

            // Apply damage when trail reaches the hit position (about 80% of the way there for responsiveness)
            if (!damageApplied && target != null && t >= 0.8f)
            {
                ApplyDamageToTarget(target, damage, hitPosition);
                damageApplied = true;
            }

            yield return null;
        }

        // Ensure trail reaches exact end position
        trail.SetPosition(1, endPos);

        // Apply damage if it wasn't applied during animation (safety check)
        if (!damageApplied && target != null && !target.IsDead())
        {
            ApplyDamageToTarget(target, damage, hitPosition);
        }

        // Keep the trail visible for a moment before returning to pool
        yield return new WaitForSeconds(bulletTrailDuration);

        // Return trail to pool
        ReturnTrailToPool(trail);
    }

    /// <summary>
    /// Helper method to apply damage and spawn hit effects
    /// </summary>
    private void ApplyDamageToTarget(IDamageable target, int damage, Vector3 hitPosition)
    {
        if (target == null || target.IsDead()) return;

        target.TakeDamage(damage);

        if (currentWeapon.hitEffect != null && hitPosition != Vector3.zero)
        {
            Instantiate(currentWeapon.hitEffect, hitPosition, Quaternion.identity);
        }

        CustomCrosshair crosshair = FindObjectOfType<CustomCrosshair>();
        if (crosshair != null)
        {
            crosshair.ShowHitFeedback();
        }

        Debug.Log($"Damaged {target.GetGameObject().name} for {damage} damage with {currentWeapon.weaponName}");
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
        FlashMuzzleLight(); 

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

    /// <summary>
    /// Flashes the muzzle light briefly when shooting
    /// </summary>
    private void FlashMuzzleLight()
    {
        if (muzzleFlashLight == null) return;

        // Stop existing flash if running
        if (currentLightFlash != null)
        {
            StopCoroutine(currentLightFlash);
        }

        // Start new flash and store reference
        currentLightFlash = StartCoroutine(MuzzleLightFlashCoroutine());
    }

    /// <summary>
    /// Coroutine that handles the light flash timing
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
        currentLightFlash = null;
    }



    public void StopAllParticles()
    {
        particlesPlaying = false;
    }

    private void OnStopShooting()
    {
        CameraZoomOnSpeed.Instance.StopFiring();
        StopAllParticles();

       
        if (muzzleFlashLight != null)
        {
            //muzzleFlashLight.enabled = false;
        }
    }


    public WeaponData GetCurrentWeapon() { return currentWeapon; }
    public string GetCurrentWeaponName() { return currentWeapon?.weaponName ?? "None"; }
    public bool IsShooting() { return wasShooting; }

    /// <summary>
    /// Returns the direction from player to mouse (for rotation script compatibility)
    /// </summary>
    public Vector2 GetShootingDirection()
    {
        return GetMouseAimDirection();
    }

    public int GetTargetsInCone() { return damageableTargets.Count; }
}
