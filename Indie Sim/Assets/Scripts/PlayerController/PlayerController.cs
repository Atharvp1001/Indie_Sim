using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; // ✅ NEW — needed for Image
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

    // ✅ NEW — drag your dash light icon Image here in Inspector
    [Header("Dash Cooldown UI")]
    [SerializeField] private Image dashLightIcon; // Image Type: Filled, Radial360, Top

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
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float wallCheckDistance = 0.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new PlayerControls();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Dash.performed += ctx => TryDash();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

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

        // ✅ NEW — start fully ready
        SetDashFill(1f);
    }

    public void SetSpeed(float newSpeed) => currentMoveSpeed = newSpeed;

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
            if (safeDashDistance < 1f) return;
        }

        StartCoroutine(DashCoroutine(safeDashDistance));
    }

    private IEnumerator DashCoroutine(float maxDistance)
    {
        isDashing = true;
        canDash = false;
        dashTimeRemaining = dashDuration;

        // ✅ NEW — instantly empty the icon when dash starts
        SetDashFill(0f);

        float distanceTraveled = 0f;

        while (dashTimeRemaining > 0 && distanceTraveled < maxDistance)
        {
            dashTimeRemaining -= Time.fixedDeltaTime;

            float frameDistance = dashSpeed * Time.fixedDeltaTime;
            if (distanceTraveled + frameDistance > maxDistance)
                frameDistance = maxDistance - distanceTraveled;

            RaycastHit2D immediateHit = Physics2D.Raycast(
                transform.position,
                dashDirection,
                frameDistance + 0.1f,
                collisionMask
            );

            if (immediateHit.collider != null) break;

            rb.linearVelocity = dashDirection * dashSpeed;
            distanceTraveled += frameDistance;

            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
        rb.linearVelocity = Vector2.zero;

        // ✅ NEW — refill the icon smoothly over dashCooldown duration
        float elapsed = 0f;
        while (elapsed < dashCooldown)
        {
            elapsed += Time.deltaTime;
            SetDashFill(Mathf.Clamp01(elapsed / dashCooldown));
            yield return null;
        }

        // ✅ NEW — ensure perfect fill at end
        SetDashFill(1f);
        canDash = true;
    }

    #endregion

    // ✅ NEW — sets fill on the dash light icon
    private void SetDashFill(float amount)
    {
        if (dashLightIcon != null)
            dashLightIcon.fillAmount = amount;
    }

    void FixedUpdate()
    {
        if (isDashing) return;

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

    public bool IsDashing() => isDashing;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pushRadius);

        if (moveInput.magnitude > 0.1f)
        {
            Gizmos.color = canDash ? Color.green : Color.red;
            Gizmos.DrawRay(transform.position, moveInput.normalized * dashSpeed * dashDuration);
        }
    }
}
