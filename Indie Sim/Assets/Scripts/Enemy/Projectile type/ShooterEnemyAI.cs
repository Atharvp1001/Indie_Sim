using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
public class ShooterEnemyAI : MonoBehaviour
{
    [Header("Detection & Range")]
    [SerializeField] private float firingRange = 6f; // Distance to stop and shoot
    [SerializeField] private float minimumDistance = 3f; // Personal space during cooldown

    [Header("Attack Settings")]
    [SerializeField] private int bulletsToFire = 6; // Number of bullets in circle
    [SerializeField] private float delayBetweenBullets = 0.08f; // "ratatata" speed
    [SerializeField] private float attackCooldown = 2f; // Time before can attack again

    [Header("Cooldown Movement")]
    [SerializeField] private float randomMoveSpeed = 1.5f; // Speed during random movement
    [SerializeField] private float randomMoveInterval = 1f; // Change direction every X seconds

    [Header("Prefab")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    // State machine
    public enum State { Idle, Chasing, Attacking, Cooldown }
    private State currentState = State.Idle;

    // References
    private Transform player;
    private EnemyMovement enemyMovement;
    private Rigidbody2D rb;
    private bool isActivated = false;

    // Timers
    private float cooldownTimer = 0f;
    private float randomMoveTimer = 0f;
    private Vector2 randomMoveDirection;

    // Attack state
    private bool isAttacking = false;

    void Start()
    {
        // Get references
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        enemyMovement = GetComponent<EnemyMovement>();
        rb = GetComponent<Rigidbody2D>();

        if (player == null)
        {
            Debug.LogError($"ShooterEnemy '{gameObject.name}': No player found!");
        }

        if (bulletPrefab == null)
        {
            Debug.LogError($"ShooterEnemy '{gameObject.name}': Bullet prefab not assigned!");
        }

        // Set initial random direction
        randomMoveDirection = Random.insideUnitCircle.normalized;
    }

    void Update()
    {
        if (player == null) return;

        // Check activation
        if (!isActivated && ActivateEnemies.Instance != null)
        {
            isActivated = ActivateEnemies.Instance.IsEnemyActivated(gameObject);
            if (!isActivated)
            {
                currentState = State.Idle;
                return;
            }
        }

        // Update state machine
        UpdateStateMachine();
    }

    void UpdateStateMachine()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:
                HandleIdleState(distanceToPlayer);
                break;

            case State.Chasing:
                HandleChasingState(distanceToPlayer);
                break;

            case State.Attacking:
                HandleAttackingState();
                break;

            case State.Cooldown:
                HandleCooldownState(distanceToPlayer);
                break;
        }
    }

    void HandleIdleState(float distanceToPlayer)
    {
        // Just wait for activation
        // EnemyMovement handles this already
        if (isActivated)
        {
            currentState = State.Chasing;
        }
    }

    void HandleChasingState(float distanceToPlayer)
    {
        // Let EnemyMovement handle the movement toward player
        enemyMovement.enabled = true;

        // Check if in firing range
        if (distanceToPlayer <= firingRange)
        {
            // Stop moving and attack
            currentState = State.Attacking;
            StartCoroutine(PerformAttack());
        }
    }

    void HandleAttackingState()
    {
        // Disable movement during attack
        enemyMovement.enabled = false;
        rb.linearVelocity = Vector2.zero; // Stop completely

        // Attack coroutine handles the actual shooting
        // State will change after attack completes
    }

    void HandleCooldownState(float distanceToPlayer)
    {
        // Disable normal chase behavior
        enemyMovement.enabled = false;

        // Countdown cooldown timer
        cooldownTimer -= Time.deltaTime;

        // Handle movement during cooldown
        if (distanceToPlayer < minimumDistance)
        {
            // Too close - back away from player
            Vector2 awayFromPlayer = ((Vector2)transform.position - (Vector2)player.position).normalized;
            rb.linearVelocity = awayFromPlayer * randomMoveSpeed;
        }
        else
        {
            // Random wandering
            randomMoveTimer -= Time.deltaTime;
            if (randomMoveTimer <= 0f)
            {
                // Pick new random direction
                randomMoveDirection = Random.insideUnitCircle.normalized;
                randomMoveTimer = randomMoveInterval;
            }

            rb.linearVelocity = randomMoveDirection * randomMoveSpeed;
        }

        // Check if cooldown finished
        if (cooldownTimer <= 0f)
        {
            currentState = State.Chasing;
            enemyMovement.enabled = true;
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;

        // Calculate angle between each bullet (360 / 6 = 60 degrees)
        float angleStep = 360f / bulletsToFire;
        float startAngle = 0f; // Can randomize this if you want

        for (int i = 0; i < bulletsToFire; i++)
        {
            // Calculate bullet direction
            float currentAngle = startAngle + (angleStep * i);
            float radians = currentAngle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            // Spawn bullet
            SpawnBullet(direction);

            // Wait before next bullet (the "ratatata" delay)
            yield return new WaitForSeconds(delayBetweenBullets);
        }

        // Attack finished - enter cooldown
        isAttacking = false;
        cooldownTimer = attackCooldown;
        currentState = State.Cooldown;
    }

    void SpawnBullet(Vector2 direction)
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning($"ShooterEnemy '{gameObject.name}': Can't spawn bullet - no prefab!");
            return;
        }

        // Spawn bullet at enemy position
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        // Initialize bullet with direction
        BulletProjectile bulletScript = bullet.GetComponent<BulletProjectile>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(direction);
        }
        else
        {
            Debug.LogWarning($"ShooterEnemy '{gameObject.name}': Bullet prefab missing BulletProjectile script!");
        }
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Firing range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, firingRange);

        // Minimum distance (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minimumDistance);

        // Current state label
        if (Application.isPlaying)
        {
            UnityEngine.GUI.color = Color.white;
            // State will show in inspector instead
        }
    }

    void OnDrawGizmosSelected()
    {
        // Show bullet directions preview
        if (!Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        float angleStep = 360f / bulletsToFire;
        for (int i = 0; i < bulletsToFire; i++)
        {
            float angle = angleStep * i;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Gizmos.DrawRay(transform.position, dir * 2f);
        }
    }

    // Public API for debugging
    public State GetCurrentState() => currentState;
    public bool IsOnCooldown() => currentState == State.Cooldown;
}