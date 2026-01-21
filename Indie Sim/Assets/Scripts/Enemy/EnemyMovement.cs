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
    
    [Header("Tile Awareness")]
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float pathCheckDistance = 2f;
    
    [Header("Pathfinding")]
    [SerializeField] private float pathUpdateInterval = 0.5f; // How often to recalculate path
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
    
    // Pathfinding enabled flag - only leaders pathfind
    private bool isPathfindingEnabled = false;
    
    // Static list to track all active enemies
    private static List<EnemyMovement> allEnemies = new List<EnemyMovement>();
    
    // Cached arrays for physics queries
    private static Collider2D[] neighborBuffer = new Collider2D[20];

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        
        rb = GetComponent<Rigidbody2D>();
        
        // Register this enemy
        if (!allEnemies.Contains(this))
            allEnemies.Add(this);
    }

    void OnDestroy()
    {
        // Unregister this enemy
        allEnemies.Remove(this);
        
        // If this was a leader, disband the flock
        if (isLeader)
        {
            foreach (var member in flockMembers)
            {
                if (member != null)
                    member.flockLeader = null;
            }
        }
        
        // Tell ActivateEnemiesAdvanced to stop pathfinding for us
        if (isPathfindingEnabled && ActivateEnemiesAdvanced.Instance != null)
        {
            ActivateEnemiesAdvanced.Instance.UnregisterPathfindingEnemy(this);
        }
    }

    void Update()
    {
        if (player == null) return;

        // Check activation
        if (!isActivated && ActivateEnemiesAdvanced.Instance != null)
        {
            isActivated = ActivateEnemiesAdvanced.Instance.IsEnemyActivated(gameObject);
            if (!isActivated) return;
        }

        // Handle knockback recovery
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
        
        // Periodically check for flock formation
        leaderCheckTimer -= Time.deltaTime;
        if (leaderCheckTimer <= 0f)
        {
            leaderCheckTimer = leaderCheckInterval;
            CheckFlockFormation();
        }
        
        // Update pathfinding if this is a leader with pathfinding enabled
        if (isLeader && isPathfindingEnabled)
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

        // Calculate desired movement direction
        Vector2 desiredDirection = CalculateFlockingBehavior();
        
        // Check if path is clear (2-tile wide requirement)
        if (!IsPathClear(desiredDirection))
        {
            // Try to find alternative path
            desiredDirection = FindAlternativePath(desiredDirection);
            
            // If still no valid path and we're pathfinding, disable pathfinding
            if (desiredDirection == Vector2.zero && isPathfindingEnabled)
            {
                DisablePathfinding();
            }
        }
        
        // Apply movement
        Vector2 targetVelocity = desiredDirection.normalized * speed;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 5f);
    }

    void UpdatePathfinding()
    {
        if (player == null || !isPathfindingEnabled) return;
        
        // Get next position from JumpFloodPathfinding
        if (JumpFloodPathfinding.Instance != null)
        {
            Vector2 nextStep = JumpFloodPathfinding.Instance.GetNextStep(transform.position, player.position);
            
            // Check if the path requires less than 2 tiles width
            Vector2 directionToNext = (nextStep - (Vector2)transform.position).normalized;
            
            if (!IsPathClear(directionToNext))
            {
                // Path is too narrow (less than 2 tiles), disable pathfinding
                Debug.Log($"Enemy {gameObject.name}: Path too narrow, disabling pathfinding");
                DisablePathfinding();
                pathfindTarget = null;
                hasValidPath = false;
            }
            else
            {
                pathfindTarget = nextStep;
                hasValidPath = true;
            }
        }
    }

    Vector2 CalculateFlockingBehavior()
    {
        // If we're a leader with pathfinding, use the pathfinding target
        if (isLeader && isPathfindingEnabled && hasValidPath && pathfindTarget.HasValue)
        {
            Vector2 pathDirection = (pathfindTarget.Value - (Vector2)transform.position).normalized;
            Vector2 separation = CalculateSeparation();
            return (pathDirection * playerSeekWeight + separation * separationWeight).normalized;
        }
        
        // If we're a flock member, follow the leader
        if (!isLeader && flockLeader != null)
        {
            Vector2 toLeader = (flockLeader.transform.position - transform.position).normalized;
            Vector2 alignment = CalculateAlignment();
            Vector2 cohesion = CalculateCohesion();
            Vector2 separation = CalculateSeparation();
            
            return (toLeader * playerSeekWeight + 
                    alignment * alignmentWeight + 
                    cohesion * cohesionWeight + 
                    separation * separationWeight).normalized;
        }
        
        // Default behavior: move toward player with separation
        Vector2 playerDirection = (player.position - transform.position).normalized;
        Vector2 sep = CalculateSeparation();
        return (playerDirection * playerSeekWeight + sep * separationWeight).normalized;
    }

    Vector2 CalculateAlignment()
    {
        Vector2 averageDirection = Vector2.zero;
        int count = 0;
        
        foreach (var enemy in GetNearbyEnemies(detectionRadius))
        {
            if (enemy.flockLeader == flockLeader)
            {
                averageDirection += enemy.rb.linearVelocity.normalized;
                count++;
            }
        }
        
        if (count > 0)
            return averageDirection / count;
        
        return Vector2.zero;
    }

    Vector2 CalculateCohesion()
    {
        Vector2 centerOfMass = Vector2.zero;
        int count = 0;
        
        foreach (var enemy in GetNearbyEnemies(detectionRadius))
        {
            if (enemy.flockLeader == flockLeader)
            {
                centerOfMass += (Vector2)enemy.transform.position;
                count++;
            }
        }
        
        if (count > 0)
        {
            centerOfMass /= count;
            return (centerOfMass - (Vector2)transform.position).normalized;
        }
        
        return Vector2.zero;
    }

    Vector2 CalculateSeparation()
    {
        Vector2 separationForce = Vector2.zero;
        
        foreach (var enemy in GetNearbyEnemies(separationRadius))
        {
            Vector2 awayFromNeighbor = (Vector2)(transform.position - enemy.transform.position);
            float distance = awayFromNeighbor.magnitude;
            
            if (distance > 0)
            {
                separationForce += awayFromNeighbor.normalized / distance;
            }
        }
        
        return separationForce;
    }

    void CheckFlockFormation()
    {
        List<EnemyMovement> nearbyEnemies = GetNearbyEnemies(leaderRadius);
        
        // Formation logic
        if (nearbyEnemies.Count >= minFlockSize - 1 && !isLeader && flockLeader == null)
        {
            BecomeLeader(nearbyEnemies);
        }
        else if (isLeader && flockMembers.Count < minFlockSize / 2)
        {
            DisbandFlock();
        }
        else if (!isLeader && flockLeader == null)
        {
            TryJoinNearbyFlock();
        }
        
        // Check if we should transfer pathfinding when joining/forming flocks
        if (isLeader && !isPathfindingEnabled)
        {
            // Try to claim a pathfinding slot
            if (ActivateEnemiesAdvanced.Instance != null)
            {
                ActivateEnemiesAdvanced.Instance.TryEnablePathfinding(this);
            }
        }
        else if (!isLeader && isPathfindingEnabled)
        {
            // We lost leadership, disable pathfinding
            DisablePathfinding();
        }
    }

    void BecomeLeader(List<EnemyMovement> nearbyEnemies)
    {
        isLeader = true;
        flockMembers.Clear();
        
        // Collect pathfinding members and disable their pathfinding
        foreach (var enemy in nearbyEnemies)
        {
            if (enemy.flockLeader == null)
            {
                // If this enemy was pathfinding, disable it
                if (enemy.isPathfindingEnabled)
                {
                    enemy.DisablePathfinding();
                }
                
                enemy.flockLeader = this;
                flockMembers.Add(enemy);
            }
        }
        
        // Try to enable pathfinding for this new leader
        if (ActivateEnemiesAdvanced.Instance != null)
        {
            ActivateEnemiesAdvanced.Instance.TryEnablePathfinding(this);
        }
        
        Debug.Log($"Enemy {gameObject.name} became flock leader with {flockMembers.Count} members");
    }

    void DisbandFlock()
    {
        isLeader = false;
        
        // Disable pathfinding when disbanding
        if (isPathfindingEnabled)
        {
            DisablePathfinding();
        }
        
        foreach (var member in flockMembers)
        {
            if (member != null)
            {
                member.flockLeader = null;
                
                // Give disbanded members a chance to pathfind
                if (ActivateEnemiesAdvanced.Instance != null)
                {
                    ActivateEnemiesAdvanced.Instance.TryEnablePathfinding(member);
                }
            }
        }
        
        flockMembers.Clear();
        Debug.Log($"Enemy {gameObject.name} disbanded flock");
    }

    void TryJoinNearbyFlock()
    {
        foreach (var enemy in GetNearbyEnemies(leaderRadius))
        {
            if (enemy.isLeader && enemy.flockMembers.Count < minFlockSize * 2)
            {
                // Disable our pathfinding if we're joining a flock
                if (isPathfindingEnabled)
                {
                    DisablePathfinding();
                }
                
                flockLeader = enemy;
                enemy.flockMembers.Add(this);
                Debug.Log($"Enemy {gameObject.name} joined flock led by {enemy.gameObject.name}");
                return;
            }
        }
    }

    List<EnemyMovement> GetNearbyEnemies(float radius)
    {
        List<EnemyMovement> nearby = new List<EnemyMovement>();
        
        // Clean up null references while iterating
        allEnemies.RemoveAll(e => e == null);
        
        foreach (var enemy in allEnemies)
        {
            if (enemy == this || enemy == null) continue;
            
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance <= radius)
                nearby.Add(enemy);
        }
        
        return nearby;
    }

    bool IsPathClear(Vector2 direction)
    {
        if (direction == Vector2.zero) return false;
        
        Vector2 currentPos = transform.position;
        
        // Check center path
        RaycastHit2D centerHit = Physics2D.Raycast(currentPos, direction, pathCheckDistance, obstacleLayer);
        
        // Check left and right sides (perpendicular to movement direction)
        Vector2 perpendicular = new Vector2(-direction.y, direction.x).normalized;
        Vector2 leftOffset = perpendicular * tileSize;
        Vector2 rightOffset = -perpendicular * tileSize;
        
        RaycastHit2D leftHit = Physics2D.Raycast(currentPos + leftOffset, direction, pathCheckDistance, obstacleLayer);
        RaycastHit2D rightHit = Physics2D.Raycast(currentPos + rightOffset, direction, pathCheckDistance, obstacleLayer);
        
        // Path is clear if center and at least one side is clear
        return !centerHit.collider && (!leftHit.collider || !rightHit.collider);
    }

    Vector2 FindAlternativePath(Vector2 blockedDirection)
    {
        float[] angles = { 30f, -30f, 60f, -60f, 90f, -90f };
        
        foreach (float angle in angles)
        {
            Vector2 testDirection = Rotate(blockedDirection, angle);
            
            if (IsPathClear(testDirection))
                return testDirection;
        }
        
        return Vector2.zero;
    }

    Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    public void ApplyKnockback(Vector2 force)
    {
        rb.linearVelocity = Vector2.zero;
        rb.linearDamping = knockbackDrag;
        rb.AddForce(force, ForceMode2D.Impulse);
        isKnockedBack = true;
        knockbackTimer = knockbackRecoveryTime;
    }

    // Called by ActivateEnemies when granting pathfinding permission
    public void EnablePathfinding()
    {
        isPathfindingEnabled = true;
        Debug.Log($"Enemy {gameObject.name}: Pathfinding ENABLED");
    }

    public void DisablePathfinding()
    {
        if (!isPathfindingEnabled) return;
        
        isPathfindingEnabled = false;
        pathfindTarget = null;
        hasValidPath = false;
        
        // Notify ActivateEnemiesAdvanced
        if (ActivateEnemiesAdvanced.Instance != null)
        {
            ActivateEnemiesAdvanced.Instance.UnregisterPathfindingEnemy(this);
        }
        
        Debug.Log($"Enemy {gameObject.name}: Pathfinding DISABLED");
    }

    public bool IsPathfinding()
    {
        return isPathfindingEnabled;
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw separation radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
        
        // Draw leader radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, leaderRadius);
        
        // Draw flock connections
        if (isLeader)
        {
            Gizmos.color = Color.green;
            foreach (var member in flockMembers)
            {
                if (member != null)
                    Gizmos.DrawLine(transform.position, member.transform.position);
            }
        }
        else if (flockLeader != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, flockLeader.transform.position);
        }
        
        // Draw pathfinding indicator
        if (isPathfindingEnabled)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
            
            if (hasValidPath && pathfindTarget.HasValue)
            {
                Gizmos.DrawLine(transform.position, pathfindTarget.Value);
            }
        }
    }
}