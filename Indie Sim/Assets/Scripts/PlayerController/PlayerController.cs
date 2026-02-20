using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseMoveSpeed = 10f;

    private Rigidbody2D rb;
    private float currentMoveSpeed;
    private Vector2 moveInput;
    private PlayerControls inputActions;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 30f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private bool invulnerableDuringDash = false;
    
    private bool isDashing = false;
    private bool canDash = true;
    private Vector2 dashDirection;
    private float dashTimeRemaining;

    [Header("Stomp Settings")]
    [SerializeField] private float stompRadius = 5f;
    [SerializeField] private int stompDamage = 25;
    [SerializeField] private float stompPushBeyondRadius = 1.5f; // How far beyond radius to push enemies
    [SerializeField] private float stompCooldown = 2f;
    [SerializeField] private bool stompCostCoins = false;
    [SerializeField] private int stompCoinCost = 10;
    [SerializeField] private LayerMask stompEnemyLayer;
    [SerializeField] private LayerMask stompWallLayer;
    [SerializeField] private LayerMask stompBulletLayer;
    [SerializeField] private GameObject stompVFXPrefab; // Assign the StompShockwave prefab
    
    private bool canStomp = true;

    [Header("Bulldozer")]
    [SerializeField] private float pushRadius = 2f;
    [SerializeField] private float pushStr = 5f;
    [SerializeField] private LayerMask enemyLayer;
    private ContactFilter2D enemyFilter;
    private List<Collider2D> pushResults = new List<Collider2D>();

    [Header("Collision Safety")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float wallCheckDistance = 0.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new PlayerControls();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        
        // Bind dash input
        inputActions.Player.Dash.performed += ctx => TryDash();
        
        // Bind stomp input
        inputActions.Player.Stomp.performed += ctx => TryStomp();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        currentMoveSpeed = baseMoveSpeed;

        rb.gravityScale = 0;
        rb.linearDamping = 0;
        rb.angularDamping = 0;
        rb.interpolation = RigidbodyInterpolation2D.None;

        enemyFilter = new ContactFilter2D();
        enemyFilter.SetLayerMask(enemyLayer);
        enemyFilter.useLayerMask = true;
    }

    public void SetSpeed(float newSpeed)
    {
        currentMoveSpeed = newSpeed;
    }

    #region Dash System

    private void TryDash()
    {
        if (!canDash || isDashing || moveInput.magnitude < 0.1f)
            return;

        dashDirection = moveInput.normalized;
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            dashDirection, 
            dashSpeed * dashDuration, 
            collisionMask
        );

        float safeDashDistance = dashSpeed * dashDuration;
        if (hit.collider != null)
        {
            safeDashDistance = Mathf.Max(0, hit.distance - wallCheckDistance);
            
            if (safeDashDistance < 1f)
                return;
        }

        StartCoroutine(DashCoroutine(safeDashDistance));
    }

    private IEnumerator DashCoroutine(float maxDistance)
    {
        isDashing = true;
        canDash = false;
        dashTimeRemaining = dashDuration;

        Vector2 startPos = transform.position;
        float distanceTraveled = 0f;

        if (invulnerableDuringDash)
        {
            // Implement invulnerability if needed
        }

        while (dashTimeRemaining > 0 && distanceTraveled < maxDistance)
        {
            dashTimeRemaining -= Time.fixedDeltaTime;
            
            float frameDistance = dashSpeed * Time.fixedDeltaTime;
            
            if (distanceTraveled + frameDistance > maxDistance)
            {
                frameDistance = maxDistance - distanceTraveled;
            }

            RaycastHit2D immediateHit = Physics2D.Raycast(
                transform.position,
                dashDirection,
                frameDistance + 0.1f,
                collisionMask
            );

            if (immediateHit.collider != null)
            {
                break;
            }

            rb.linearVelocity = dashDirection * dashSpeed;
            distanceTraveled += frameDistance;

            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
        rb.linearVelocity = Vector2.zero;

        if (invulnerableDuringDash)
        {
            // Restore normal layer
        }

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    #endregion

    #region Stomp System

    private void TryStomp()
    {
        // Cannot stomp while dashing or on cooldown
        if (!canStomp || isDashing)
        {
            Debug.Log("Cannot stomp: on cooldown or dashing");
            return;
        }

        // Check coin cost
        if (stompCostCoins)
        {
            if (CoinManager.Instance == null || !CoinManager.Instance.HasEnoughCoins(stompCoinCost))
            {
                Debug.Log($"Not enough coins for stomp! Need: {stompCoinCost}");
                return;
            }

            // Spend coins
            CoinManager.Instance.SpendCoins(stompCoinCost);
            Debug.Log($"Spent {stompCoinCost} coins for stomp");
        }

        // Execute stomp
        PerformStomp();

        // Start cooldown
        StartCoroutine(StompCooldown());
    }

    private void PerformStomp()
    {
        Debug.Log($"STOMP! Radius: {stompRadius}, Damage: {stompDamage}");

        Vector2 playerPos = transform.position;

        // Spawn VFX
        if (stompVFXPrefab != null)
        {
            GameObject vfx = Instantiate(stompVFXPrefab, transform.position, Quaternion.identity);
            StompShockwave shockwave = vfx.GetComponent<StompShockwave>();
            if (shockwave != null)
            {
                shockwave.Initialize(stompRadius);
            }
        }

        // 1. Destroy bullets in range
        DestroyBulletsInRange(playerPos);

        // 2. Damage and push enemies
        DamageAndPushEnemies(playerPos);
    }

    private void DestroyBulletsInRange(Vector2 playerPos)
    {
        Collider2D[] bullets = Physics2D.OverlapCircleAll(playerPos, stompRadius, stompBulletLayer);

        foreach (Collider2D bulletCol in bullets)
        {
            // Check line of sight (no walls blocking)
            Vector2 toTarget = (Vector2)bulletCol.transform.position - playerPos;
            RaycastHit2D wallCheck = Physics2D.Raycast(playerPos, toTarget.normalized, toTarget.magnitude, stompWallLayer);

            if (wallCheck.collider == null) // No wall blocking
            {
                // Get bullet component and return to pool
                Bullet bullet = bulletCol.GetComponent<Bullet>();
                if (bullet != null)
                {
                    // Bullet script has pool management, just destroy it
                    Destroy(bulletCol.gameObject);
                    Debug.Log("Destroyed bullet with stomp");
                }
                else
                {
                    // Fallback for bullets without Bullet script
                    Destroy(bulletCol.gameObject);
                }
            }
        }
    }

    private void DamageAndPushEnemies(Vector2 playerPos)
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(playerPos, stompRadius, stompEnemyLayer);

        foreach (Collider2D enemyCol in enemies)
        {
            // Check line of sight (no walls blocking)
            Vector2 toEnemy = (Vector2)enemyCol.transform.position - playerPos;
            float distanceToEnemy = toEnemy.magnitude;
            
            RaycastHit2D wallCheck = Physics2D.Raycast(playerPos, toEnemy.normalized, distanceToEnemy, stompWallLayer);

            if (wallCheck.collider != null) // Wall blocking
            {
                Debug.Log($"Enemy {enemyCol.name} blocked by wall, skipping");
                continue;
            }

            // Deal damage
            IDamageable damageable = enemyCol.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead())
            {
                damageable.TakeDamage(stompDamage);
                Debug.Log($"Stomped {enemyCol.name} for {stompDamage} damage");
            }

            // Calculate push position (beyond radius)
            Vector2 pushDirection = toEnemy.normalized;
            float targetDistance = stompRadius + stompPushBeyondRadius;
            Vector2 targetPosition = playerPos + (pushDirection * targetDistance);

            // Check if target position hits a wall
            RaycastHit2D pushWallCheck = Physics2D.Raycast(
                enemyCol.transform.position,
                pushDirection,
                Vector2.Distance(enemyCol.transform.position, targetPosition),
                stompWallLayer
            );

            if (pushWallCheck.collider != null)
            {
                // Wall in the way - push only to just before wall
                float safeDistance = pushWallCheck.distance - 0.5f; // Leave small gap
                targetPosition = (Vector2)enemyCol.transform.position + (pushDirection * safeDistance);
                Debug.Log($"Wall detected, pushed {enemyCol.name} to safe distance");
            }

            // INSTANT push - teleport enemy to target position
            enemyCol.transform.position = targetPosition;
            
            // Optional: Reset enemy velocity to prevent sliding
            Rigidbody2D enemyRb = enemyCol.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                enemyRb.linearVelocity = Vector2.zero;
            }

            Debug.Log($"Pushed {enemyCol.name} to position {targetPosition}");
        }
    }

    private IEnumerator StompCooldown()
    {
        canStomp = false;
        Debug.Log($"Stomp on cooldown for {stompCooldown} seconds");
        yield return new WaitForSeconds(stompCooldown);
        canStomp = true;
        Debug.Log("Stomp ready!");
    }

    #endregion

    void FixedUpdate()
    {
        if (isDashing)
            return;

        rb.linearVelocity = moveInput * currentMoveSpeed;

        if (moveInput.magnitude > 0.1f)
        {
            HandleBulldozerPhysics();
        }
    }

    private void HandleBulldozerPhysics()
    {
        pushResults.Clear();
        Physics2D.OverlapCircle(transform.position, pushRadius, enemyFilter, pushResults);

        foreach (Collider2D col in pushResults)
        {
            if (col == null) continue;

            Rigidbody2D enemyRb = col.attachedRigidbody;
            if (enemyRb != null)
            {
                Vector2 toEnemy = (Vector2)col.transform.position - (Vector2)transform.position;
                Vector2 awayDir = toEnemy.normalized;
                Vector2 moveDir = moveInput.normalized;
                Vector2 sideDir = new Vector2(-moveDir.y, moveDir.x);

                float dot = Vector2.Dot(sideDir, toEnemy);
                if (dot < 0) sideDir = -sideDir;

                Vector2 finalPush = (awayDir * 0.3f) + (sideDir * 0.7f);
                enemyRb.linearVelocity = finalPush.normalized * pushStr;
            }
        }
    }

    public bool IsDashing()
    {
        return isDashing;
    }

    public bool CanStomp()
    {
        return canStomp && !isDashing;
    }

    void OnDrawGizmosSelected()
    {
        // Bulldozer radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pushRadius);

        // Stomp radius
        Gizmos.color = canStomp ? Color.cyan : Color.gray;
        Gizmos.DrawWireSphere(transform.position, stompRadius);

        // Stomp push target distance
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, stompRadius + stompPushBeyondRadius);

        // Dash direction
        if (moveInput.magnitude > 0.1f)
        {
            Gizmos.color = canDash ? Color.green : Color.red;
            Vector2 dashDir = moveInput.normalized;
            Gizmos.DrawRay(transform.position, dashDir * dashSpeed * dashDuration);
        }
    }
}