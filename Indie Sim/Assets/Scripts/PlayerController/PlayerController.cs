using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseMoveSpeed = 5f; // Your default speed without upgrades

    [Header("References")]
    public FixedJoystick joystick; // Drag your joystick here in inspector

    private Rigidbody2D rb;
    private float currentMoveSpeed;

    [Header("Bulldozer")]
    [SerializeField] private float pushRadius = 2f;
    [SerializeField] private float pushStr = 5f;
    [SerializeField] private LayerMask enemyLayer;
    private Collider2D[] pushResults = new Collider2D[40];

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentMoveSpeed = baseMoveSpeed; // Initialize current speed
    }

    /// <summary>
    /// Set player movement speed
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        // If you have a speed variable, update it here
        // Example: if your speed variable is called 'moveSpeed'
        currentMoveSpeed = newSpeed;

        Debug.Log($"[PlayerMovement] Speed updated to: {newSpeed}");
    }


    void FixedUpdate()
    {
    Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    rb.linearVelocity = new Vector2(joystick.Horizontal * currentMoveSpeed, joystick.Vertical * currentMoveSpeed);

    if (moveInput.magnitude > 0.1f) 
    {
        int enemyCount = Physics2D.OverlapCircleNonAlloc(transform.position, pushRadius, pushResults, enemyLayer);

        for (int i = 0; i < enemyCount; i++)
        {
            Rigidbody2D enemyRb = pushResults[i].attachedRigidbody;
            if (enemyRb != null)
            {
                // 1. Vector from player to enemy
                Vector2 toEnemy = (Vector2)pushResults[i].transform.position - (Vector2)transform.position;
                
                // 2. The "Away" force (keeps them out of your skin)
                Vector2 awayDir = toEnemy.normalized;

                // 3. The "Side" force (The Marble Secret)
                // We find the 'Right' vector of your movement to shove them sideways
                Vector2 moveDir = moveInput.normalized;
                Vector2 sideDir = new Vector2(-moveDir.y, moveDir.x); // Perpendicular to movement

                // Determine if the enemy is on the left or right of our path
                float dot = Vector2.Dot(sideDir, toEnemy);
                if (dot < 0) sideDir = -sideDir; // Push them to the closest side

                // 4. Combine: Push away slightly, but push sideways STRONGLY
                Vector2 finalPush = (awayDir * 0.3f) + (sideDir * 0.7f);
                
                enemyRb.linearVelocity = finalPush.normalized * pushStr;
            }
        }
    }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // This draws a yellow circle in the Scene view so you can see the push range
        Gizmos.DrawWireSphere(transform.position, pushRadius);
    }
}
