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
    [SerializeField] private bool invulnerableDuringDash = false; // Optional: make invulnerable
    
    private bool isDashing = false;
    private bool canDash = true;
    private Vector2 dashDirection;
    private float dashTimeRemaining;

    [Header("Bulldozer")]
    [SerializeField] private float pushRadius = 2f;
    [SerializeField] private float pushStr = 5f;
    [SerializeField] private LayerMask enemyLayer;
    private ContactFilter2D enemyFilter;
    private List<Collider2D> pushResults = new List<Collider2D>();

    [Header("Collision Safety")]
    [SerializeField] private LayerMask collisionMask; // Set to walls/obstacles layer
    [SerializeField] private float wallCheckDistance = 0.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new PlayerControls();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        
        // Bind dash input
        inputActions.Player.Dash.performed += ctx => TryDash();
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

        // Unity 6: Configure Rigidbody for instant movement
        rb.gravityScale = 0;
        rb.linearDamping = 0;
        rb.angularDamping = 0;
        rb.interpolation = RigidbodyInterpolation2D.None;

        // Setup contact filter for bulldozer
        enemyFilter = new ContactFilter2D();
        enemyFilter.SetLayerMask(enemyLayer);
        enemyFilter.useLayerMask = true;
    }

    public void SetSpeed(float newSpeed)
    {
        currentMoveSpeed = newSpeed;
    }

    private void TryDash()
    {
        // Can't dash if on cooldown, already dashing, or not moving
        if (!canDash || isDashing || moveInput.magnitude < 0.1f)
            return;

        // Check if there's a wall in dash direction
        dashDirection = moveInput.normalized;
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            dashDirection, 
            dashSpeed * dashDuration, 
            collisionMask
        );

        // If we'd hit a wall, calculate safe dash distance
        float safeDashDistance = dashSpeed * dashDuration;
        if (hit.collider != null)
        {
            // Dash only to just before the wall
            safeDashDistance = Mathf.Max(0, hit.distance - wallCheckDistance);
            
            // If too close to wall, don't dash
            if (safeDashDistance < 1f)
                return;
        }

        // Start dash
        StartCoroutine(DashCoroutine(safeDashDistance));
    }

    private IEnumerator DashCoroutine(float maxDistance)
    {
        isDashing = true;
        canDash = false;
        dashTimeRemaining = dashDuration;

        Vector2 startPos = transform.position;
        float distanceTraveled = 0f;

        // Optional: Disable player collision or set invulnerability
        if (invulnerableDuringDash)
        {
            // You can implement invulnerability here
            // e.g., gameObject.layer = LayerMask.NameToLayer("InvulnerablePlayer");
        }

        while (dashTimeRemaining > 0 && distanceTraveled < maxDistance)
        {
            dashTimeRemaining -= Time.fixedDeltaTime;
            
            // Calculate dash velocity
            float frameDistance = dashSpeed * Time.fixedDeltaTime;
            
            // Don't exceed max safe distance
            if (distanceTraveled + frameDistance > maxDistance)
            {
                frameDistance = maxDistance - distanceTraveled;
            }

            // Perform collision check before moving
            RaycastHit2D immediateHit = Physics2D.Raycast(
                transform.position,
                dashDirection,
                frameDistance + 0.1f,
                collisionMask
            );

            if (immediateHit.collider != null)
            {
                // Hit wall during dash - stop immediately
                break;
            }

            rb.linearVelocity = dashDirection * dashSpeed;
            distanceTraveled += frameDistance;

            yield return new WaitForFixedUpdate();
        }

        // End dash
        isDashing = false;
        rb.linearVelocity = Vector2.zero;

        // Restore collision/vulnerability
        if (invulnerableDuringDash)
        {
            // Restore normal layer
            // e.g., gameObject.layer = LayerMask.NameToLayer("Player");
        }

        // Start cooldown
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    void FixedUpdate()
    {
        // Don't allow normal movement during dash
        if (isDashing)
            return;

        // Normal movement
        rb.linearVelocity = moveInput * currentMoveSpeed;

        // Bulldozer (disabled during dash)
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

    // Public method to check if player is dashing (for damage immunity checks)
    public bool IsDashing()
    {
        return isDashing;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pushRadius);

        // Visualize dash direction if moving
        if (moveInput.magnitude > 0.1f)
        {
            Gizmos.color = canDash ? Color.green : Color.red;
            Vector2 dashDir = moveInput.normalized;
            Gizmos.DrawRay(transform.position, dashDir * dashSpeed * dashDuration);
        }
    }
}