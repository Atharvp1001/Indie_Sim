using UnityEngine;
using System.Collections.Generic;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 2f;
    public float rotationSpeed = 5f;

    [Header("Knockback Settings")]
    [SerializeField] private float normalDrag = 0f;
    [SerializeField] private float knockbackDrag = 15f;
    private bool isKnockedBack = false;
    private float knockbackRecoveryTime = 0.2f;
    private float knockbackTimer = 0f;

    [Header("Flocking Settings")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float leaderRadius = 3f;
    [SerializeField] private int minFlockSize = 6;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float playerSeekWeight = 2f;

    [Header("Collision Avoidance")]
    [Tooltip("Layer(s) the enemy should steer around — walls, props, anything you want them to avoid.")]
    [SerializeField] private LayerMask avoidanceLayer;
    [Tooltip("How far ahead (world units) to sense obstacles.")]
    [SerializeField] private float avoidanceDistance = 1.5f;
    [Tooltip("How strongly to steer away — higher values = snappier avoidance.")]
    [SerializeField] private float avoidanceWeight = 3f;
    [Tooltip("Number of rays in the detection fan. 5-7 works well for most enemies.")]
    [SerializeField] private int avoidanceRayCount = 5;
    [Tooltip("Total arc of the detection fan in degrees. 120 covers a wide forward cone.")]
    [SerializeField] private float avoidanceArcDegrees = 120f;

    [Header("Tile Awareness")]
    [SerializeField] private float tileSize = 1f;
    [Tooltip("Obstacle layer used by JFA path-clear checks. Can be the same as Avoidance Layer.")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float pathCheckDistance = 2f;

    [Header("Pathfinding")]
    [Tooltip("Enable JumpFlood pathfinding. Uncheck to use direct sightline + boids only.")]
    [SerializeField] private bool usePathfinding = true;
    [SerializeField] private float pathUpdateInterval = 0.5f;
    private Vector2? pathfindTarget = null;
    private bool hasValidPath = false;
    private float pathUpdateTimer = 0f;

    private Transform player;
    private Rigidbody2D rb;
    private bool isActivated = false;

    // Flocking data
    private EnemyMovement flockLeader;
    private List<EnemyMovement> flockMembers = new List<EnemyMovement>();
    private bool isLeader = false;
    private float leaderCheckTimer = 0f;
    private const float leaderCheckInterval = 1f;

    // Runtime pathfinding flag — only leaders pathfind, and only if usePathfinding is true
    private bool isPathfindingEnabled = false;

    private static List<EnemyMovement> allEnemies = new List<EnemyMovement>();

    // -------------------------------------------------------------------------
    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        rb = GetComponent<Rigidbody2D>();

        if (!allEnemies.Contains(this))
            allEnemies.Add(this);
    }

    void OnDestroy()
    {
        allEnemies.Remove(this);

        if (isLeader)
        {
            foreach (var member in flockMembers)
                if (member != null) member.flockLeader = null;
            flockMembers.Clear();
        }

        if (flockLeader != null && flockLeader.IsLeader())
            flockLeader.flockMembers.Remove(this);

        if (isPathfindingEnabled && ActivateEnemies.Instance != null)
            ActivateEnemies.Instance.UnregisterPathfindingEnemy(this);
    }

    // -------------------------------------------------------------------------
    void Update()
    {
        if (player == null) return;

        if (!isActivated && ActivateEnemies.Instance != null)
        {
            isActivated = ActivateEnemies.Instance.IsEnemyActivated(gameObject);
            if (!isActivated) return;
        }

        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
                rb.linearVelocity = Vector2.zero;
                rb.linearDamping = normalDrag;
            }
        }

        leaderCheckTimer -= Time.deltaTime;
        if (leaderCheckTimer <= 0f)
        {
            leaderCheckTimer = leaderCheckInterval;
            CheckFlockFormation();
        }

        if (usePathfinding && isLeader && isPathfindingEnabled)
        {
            pathUpdateTimer -= Time.deltaTime;
            if (pathUpdateTimer <= 0f)
            {
                pathUpdateTimer = pathUpdateInterval;
                UpdatePathfinding();
            }
        }
    }

    void FixedUpdate()
    {
        if (!isActivated || isKnockedBack || player == null) return;

        // 1. Desired direction from flocking / pathfinding
        Vector2 desiredDirection = CalculateFlockingBehavior();

        // 2. Blend in obstacle avoidance steering (always active regardless of pathfinding toggle)
        Vector2 avoidance = CalculateAvoidanceSteering(desiredDirection);
        if (avoidance != Vector2.zero)
            desiredDirection = (desiredDirection + avoidance * avoidanceWeight).normalized;

        // 3. JFA path-clear check (only relevant when pathfinding is on)
        if (usePathfinding && !IsPathClear(desiredDirection))
        {
            Vector2 alt = FindAlternativePath(desiredDirection);
            if (alt == Vector2.zero && isPathfindingEnabled)
                DisablePathfinding();
            else if (alt != Vector2.zero)
                desiredDirection = alt;
        }

        // 4. Apply velocity
        rb.linearVelocity = Vector2.Lerp(
            rb.linearVelocity,
            desiredDirection.normalized * speed,
            Time.fixedDeltaTime * 5f);
    }

    // -------------------------------------------------------------------------
    // Collision Avoidance
    // -------------------------------------------------------------------------

    /// <summary>
    /// Casts a fan of rays ahead of the enemy and returns a weighted steering
    /// force that pushes it away from obstacles on avoidanceLayer.
    /// Active in both pathfinding and sightline modes.
    /// </summary>
    Vector2 CalculateAvoidanceSteering(Vector2 moveDirection)
    {
        if (moveDirection == Vector2.zero || avoidanceRayCount == 0) return Vector2.zero;

        Vector2 steer = Vector2.zero;
        float halfArc   = avoidanceArcDegrees * 0.5f;
        float angleStep = avoidanceRayCount > 1
            ? avoidanceArcDegrees / (avoidanceRayCount - 1)
            : 0f;

        for (int i = 0; i < avoidanceRayCount; i++)
        {
            float   angle  = -halfArc + angleStep * i;
            Vector2 rayDir = Rotate(moveDirection, angle);

            RaycastHit2D hit = Physics2D.Raycast(
                (Vector2)transform.position, rayDir, avoidanceDistance, avoidanceLayer);

            if (hit.collider != null)
            {
                // Proximity 0..1 — closer hits push harder
                float proximity = 1f - (hit.distance / avoidanceDistance);
                // Push perpendicular to the ray (left or right) rather than straight back,
                // so the enemy slides along surfaces instead of freezing in front of them.
                Vector2 perp = new Vector2(-rayDir.y, rayDir.x); // rotate 90°
                // Bias the perpendicular towards the side with more open space
                // (negative angle = left rays → steer right, and vice versa)
                steer += perp * (angle < 0f ? 1f : -1f) * proximity;
            }
        }

        return steer; // caller normalises after blending
    }

    // -------------------------------------------------------------------------
    // Flocking & Movement
    // -------------------------------------------------------------------------

    void UpdatePathfinding()
    {
        if (player == null || !isPathfindingEnabled || !usePathfinding) return;

        if (JumpFloodPathfinding.Instance != null)
        {
            Vector2 nextStep      = JumpFloodPathfinding.Instance.GetNextStep(transform.position, player.position);
            Vector2 dirToNext     = (nextStep - (Vector2)transform.position).normalized;

            if (!IsPathClear(dirToNext))
            {
                Debug.Log($"Enemy {gameObject.name}: Path too narrow, disabling pathfinding");
                DisablePathfinding();
                pathfindTarget = null;
                hasValidPath   = false;
            }
            else
            {
                pathfindTarget = nextStep;
                hasValidPath   = true;
            }
        }
    }

    Vector2 CalculateFlockingBehavior()
    {
        if (!isLeader && flockLeader != null && flockLeader == null)
            flockLeader = null;

        // --- PATHFINDING OFF: everyone steers directly to player + full boids ---
        if (!usePathfinding)
        {
            Vector2 playerDir  = (player.position - transform.position).normalized;
            Vector2 alignment  = CalculateAlignment();
            Vector2 cohesion   = CalculateCohesion();
            Vector2 separation = CalculateSeparation();

            return (playerDir  * playerSeekWeight  +
                    alignment  * alignmentWeight   +
                    cohesion   * cohesionWeight    +
                    separation * separationWeight).normalized;
        }

        // --- PATHFINDING ON ---

        // Leader with a valid JFA path
        if (isLeader && isPathfindingEnabled && hasValidPath && pathfindTarget.HasValue)
        {
            Vector2 pathDir    = (pathfindTarget.Value - (Vector2)transform.position).normalized;
            Vector2 separation = CalculateSeparation();
            return (pathDir * playerSeekWeight + separation * separationWeight).normalized;
        }

        // Flock member — follow leader
        if (!isLeader && flockLeader != null)
        {
            Vector2 toLeader   = (flockLeader.transform.position - transform.position).normalized;
            Vector2 alignment  = CalculateAlignment();
            Vector2 cohesion   = CalculateCohesion();
            Vector2 separation = CalculateSeparation();

            return (toLeader   * playerSeekWeight  +
                    alignment  * alignmentWeight   +
                    cohesion   * cohesionWeight    +
                    separation * separationWeight).normalized;
        }

        // Pathfinding ON but no leader/path yet — fallback direct to player
        Vector2 pd  = (player.position - transform.position).normalized;
        Vector2 sep = CalculateSeparation();
        return (pd * playerSeekWeight + sep * separationWeight).normalized;
    }

    Vector2 CalculateAlignment()
    {
        Vector2 avg = Vector2.zero;
        int count = 0;
        foreach (var enemy in GetNearbyEnemies(detectionRadius))
        {
            if (enemy == null) continue;
            if (enemy.flockLeader == flockLeader) { avg += enemy.rb.linearVelocity.normalized; count++; }
        }
        return count > 0 ? avg / count : Vector2.zero;
    }

    Vector2 CalculateCohesion()
    {
        Vector2 center = Vector2.zero;
        int count = 0;
        foreach (var enemy in GetNearbyEnemies(detectionRadius))
        {
            if (enemy == null) continue;
            if (enemy.flockLeader == flockLeader) { center += (Vector2)enemy.transform.position; count++; }
        }
        if (count > 0) { center /= count; return (center - (Vector2)transform.position).normalized; }
        return Vector2.zero;
    }

    Vector2 CalculateSeparation()
    {
        Vector2 force = Vector2.zero;
        foreach (var enemy in GetNearbyEnemies(separationRadius))
        {
            if (enemy == null) continue;
            Vector2 away = (Vector2)(transform.position - enemy.transform.position);
            float dist = away.magnitude;
            if (dist > 0) force += away.normalized / dist;
        }
        return force;
    }

    // -------------------------------------------------------------------------
    // Flock Management
    // -------------------------------------------------------------------------

    void CheckFlockFormation()
    {
        if (isLeader) flockMembers.RemoveAll(m => m == null);

        List<EnemyMovement> nearby = GetNearbyEnemies(leaderRadius);

        if (nearby.Count >= minFlockSize - 1 && !isLeader && flockLeader == null)
            BecomeLeader(nearby);
        else if (isLeader && flockMembers.Count < minFlockSize / 2)
            DisbandFlock();
        else if (!isLeader && flockLeader == null)
            TryJoinNearbyFlock();

        if (!isLeader && isPathfindingEnabled)
            DisablePathfinding();
    }

    void BecomeLeader(List<EnemyMovement> nearbyEnemies)
    {
        isLeader = true;
        flockMembers.Clear();
        foreach (var enemy in nearbyEnemies)
        {
            if (enemy == null) continue;
            if (enemy.flockLeader == null)
            {
                if (enemy.isPathfindingEnabled) enemy.DisablePathfinding();
                enemy.flockLeader = this;
                flockMembers.Add(enemy);
            }
        }
        Debug.Log($"Enemy {gameObject.name} became leader with {flockMembers.Count} members");
    }

    void DisbandFlock()
    {
        isLeader = false;
        if (isPathfindingEnabled) DisablePathfinding();
        foreach (var member in flockMembers)
            if (member != null) member.flockLeader = null;
        flockMembers.Clear();
        Debug.Log($"Enemy {gameObject.name} disbanded flock");
    }

    void TryJoinNearbyFlock()
    {
        foreach (var enemy in GetNearbyEnemies(leaderRadius))
        {
            if (enemy == null) continue;
            if (enemy.IsLeader() && enemy.flockMembers.Count < minFlockSize * 2)
            {
                if (isPathfindingEnabled) DisablePathfinding();
                flockLeader = enemy;
                enemy.flockMembers.Add(this);
                Debug.Log($"Enemy {gameObject.name} joined flock led by {enemy.gameObject.name}");
                return;
            }
        }
    }

    List<EnemyMovement> GetNearbyEnemies(float radius)
    {
        var nearby = new List<EnemyMovement>();
        allEnemies.RemoveAll(e => e == null);
        foreach (var enemy in allEnemies)
        {
            if (enemy == this || enemy == null) continue;
            if (Vector2.Distance(transform.position, enemy.transform.position) <= radius)
                nearby.Add(enemy);
        }
        return nearby;
    }

    // -------------------------------------------------------------------------
    // JFA Path Checks
    // -------------------------------------------------------------------------

    bool IsPathClear(Vector2 direction)
    {
        if (direction == Vector2.zero) return false;
        Vector2 pos  = transform.position;
        Vector2 perp = new Vector2(-direction.y, direction.x).normalized;

        bool center = !Physics2D.Raycast(pos,                     direction, pathCheckDistance, obstacleLayer).collider;
        bool left   = !Physics2D.Raycast(pos + perp * tileSize,   direction, pathCheckDistance, obstacleLayer).collider;
        bool right  = !Physics2D.Raycast(pos - perp * tileSize,   direction, pathCheckDistance, obstacleLayer).collider;

        return center && (left || right);
    }

    Vector2 FindAlternativePath(Vector2 blockedDirection)
    {
        float[] angles = { 30f, -30f, 60f, -60f, 90f, -90f };
        foreach (float angle in angles)
        {
            Vector2 test = Rotate(blockedDirection, angle);
            if (IsPathClear(test)) return test;
        }
        return Vector2.zero;
    }

    // -------------------------------------------------------------------------
    // Utilities
    // -------------------------------------------------------------------------

    Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    public void ApplyKnockback(Vector2 force)
    {
        rb.linearVelocity = Vector2.zero;
        rb.linearDamping = knockbackDrag;
        rb.AddForce(force, ForceMode2D.Impulse);
        isKnockedBack = true;
        knockbackTimer = knockbackRecoveryTime;
    }

    // -------------------------------------------------------------------------
    // Pathfinding Enable / Disable (called by ActivateEnemies)
    // -------------------------------------------------------------------------

    public void EnablePathfinding()
    {
        if (!usePathfinding)
        {
            Debug.Log($"Enemy {gameObject.name}: Pathfinding skipped — toggle is off in Inspector");
            return;
        }
        isPathfindingEnabled = true;
        Debug.Log($"Enemy {gameObject.name}: Pathfinding ENABLED");
    }

    public void DisablePathfinding()
    {
        if (!isPathfindingEnabled) return;
        isPathfindingEnabled = false;
        pathfindTarget = null;
        hasValidPath = false;
        if (ActivateEnemies.Instance != null)
            ActivateEnemies.Instance.UnregisterPathfindingEnemy(this);
        Debug.Log($"Enemy {gameObject.name}: Pathfinding DISABLED");
    }

    public bool IsPathfinding() => isPathfindingEnabled;
    public bool IsLeader()      => isLeader;

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    void OnDrawGizmosSelected()
    {
        // Boid radii
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, leaderRadius);

        // Avoidance fan preview (uses velocity at runtime, transform.up in edit mode)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Vector2 forward = Application.isPlaying && rb != null && rb.linearVelocity.sqrMagnitude > 0.01f
            ? rb.linearVelocity.normalized
            : (Vector2)transform.up;
        float halfArc   = avoidanceArcDegrees * 0.5f;
        float angleStep = avoidanceRayCount > 1 ? avoidanceArcDegrees / (avoidanceRayCount - 1) : 0f;
        for (int i = 0; i < avoidanceRayCount; i++)
        {
            Vector2 rayDir = Rotate(forward, -halfArc + angleStep * i);
            Gizmos.DrawRay(transform.position, rayDir * avoidanceDistance);
        }

        // Flock connections
        if (isLeader)
        {
            Gizmos.color = Color.green;
            foreach (var member in flockMembers)
                if (member != null) Gizmos.DrawLine(transform.position, member.transform.position);
        }
        else if (flockLeader != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, flockLeader.transform.position);
        }

        // Pathfinding state indicators
        if (usePathfinding && isPathfindingEnabled)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
            if (hasValidPath && pathfindTarget.HasValue)
                Gizmos.DrawLine(transform.position, pathfindTarget.Value);
        }
        if (!usePathfinding)
        {
            Gizmos.color = Color.grey;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
        }
    }
}