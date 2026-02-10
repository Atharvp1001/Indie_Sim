using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseMoveSpeed = 10f;

    private Rigidbody2D rb;
    private float currentMoveSpeed;
    private Vector2 moveInput;
    private PlayerControls inputActions;

    [Header("Bulldozer")]
    [SerializeField] private float pushRadius = 2f;
    [SerializeField] private float pushStr = 5f;
    [SerializeField] private LayerMask enemyLayer;
    private ContactFilter2D enemyFilter;
    private List<Collider2D> pushResults = new List<Collider2D>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new PlayerControls();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
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

        // ✅ Unity 6: Configure Rigidbody for instant movement
        rb.gravityScale = 0;
        rb.linearDamping = 0;      // Was: drag
        rb.angularDamping = 0;     // Was: angularDrag
        rb.interpolation = RigidbodyInterpolation2D.None;

        // ✅ IMPORTANT: Allow rotation (SimplePlayerRotation needs this)
        //rb.constraints = RigidbodyConstraints2D.None;

        // Setup contact filter for bulldozer
        enemyFilter = new ContactFilter2D();
        enemyFilter.SetLayerMask(enemyLayer);
        enemyFilter.useLayerMask = true;
    }

    public void SetSpeed(float newSpeed)
    {
        currentMoveSpeed = newSpeed;
    }

    void FixedUpdate()
    {
        // ✅ Unity 6: Use linearVelocity instead of velocity
        rb.linearVelocity = moveInput * currentMoveSpeed;

        // Bulldozer
        if (moveInput.magnitude > 0.1f)
        {
            HandleBulldozerPhysics();
        }
    }

    private void HandleBulldozerPhysics()
    {
        pushResults.Clear();

        // ✅ Unity 6: Use OverlapCircle with List instead of NonAlloc
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

                // ✅ Unity 6: Use linearVelocity
                enemyRb.linearVelocity = finalPush.normalized * pushStr;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pushRadius);
    }
}
