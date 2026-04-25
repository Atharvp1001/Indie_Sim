using UnityEngine;

[RequireComponent(typeof(BossEnemy))]
public class BossAnimator : MonoBehaviour
{
    private BossEnemy boss;
    private Animator animator;
    private Rigidbody2D rb;
    private Transform playerTransform;
    private Transform spriteTransform;

    [Header("Fine Tuning")]
    [SerializeField] private float animationSpeedMultiplier = 0.2f; // Adjust this to feel right

    void Awake()
    {
        boss = GetComponent<BossEnemy>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        
        if (animator != null) spriteTransform = animator.transform;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        if (animator == null || rb == null) return;

        // 1. Sync Speed (Calculates velocity magnitude)
        float currentVelocity = rb.linearVelocity.magnitude;
        animator.SetFloat("MoveSpeed", currentVelocity * animationSpeedMultiplier);

        // 2. State Detection 
        // If kinematic and not moving, the boss is "Stuck" (Attacking)
        bool isStuck = rb.bodyType == RigidbodyType2D.Kinematic;
        animator.SetBool("IsStuck", isStuck);

        // 3. Rotation Logic (Facing Player)
        if (isStuck && playerTransform != null && spriteTransform != null)
        {
            Vector3 dir = playerTransform.position - spriteTransform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            // Assumes sprite faces 'Up' (90 degrees). Change -90 to 0 if it faces 'Right'.
            spriteTransform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
        else if (spriteTransform != null)
        {
            // Reset rotation so it doesn't look sideways while bouncing
            spriteTransform.localRotation = Quaternion.identity;
        }
    }
}