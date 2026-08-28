using UnityEngine;

/// <summary>
/// Handles player sprite rotation and animation.
/// Reads moveInput from PlayerController — no duplicate input binding.
/// Attach to the Player GameObject (root, not sprite child).
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spriteTransform; // The sprite child GameObject
    [SerializeField] private Animator animator;          // Animator on the sprite child
    public Animator Animator => animator;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 15f;  // Higher = snappier
    [SerializeField] private bool instantRotation = false; // Toggle snap vs smooth

    [Header("Animation Parameters")]
    [SerializeField] private string isMovingParam = "IsMoving"; // Must match Animator

    // Reference to read moveInput from
    private PlayerController playerController;
    private Rigidbody2D rb;
    private float targetZRotation = 0f;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();

        if (playerController == null)
            Debug.LogError("[PlayerAnimationController] PlayerController not found on same GameObject!");
    }

    private void Update()
    {
        Vector2 moveInput = rb.linearVelocity.normalized; // ✅ Read actual velocity — works with dash too

       // HandleRotation(moveInput);
        HandleAnimation(moveInput);
    }

    private void HandleRotation(Vector2 moveInput)
    {
        if (spriteTransform == null) return;
        if (moveInput.magnitude < 0.1f) return; // Don't rotate when idle — hold last direction

        // Determine dominant direction and set target Z rotation
        if (Mathf.Abs(moveInput.x) >= Mathf.Abs(moveInput.y))
        {
            // Horizontal dominates
            if (moveInput.x > 0f)
                targetZRotation = 0f;     // D — default/right
            else
                targetZRotation = -90f;   // A — left
        }
        else
        {
            // Vertical dominates
            if (moveInput.y > 0f)
                targetZRotation = 90f;   // W — up
            else
                targetZRotation = 180f;    // S — down
        }

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetZRotation);

        if (instantRotation)
        {
            // Snap immediately
            spriteTransform.rotation = targetRotation;
        }
        else
        {
            // Smooth interpolation
            spriteTransform.rotation = Quaternion.Lerp(
                spriteTransform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private void HandleAnimation(Vector2 moveInput)
    {
        if (animator == null) return;

        bool isMoving = moveInput.magnitude > 0.1f;
        animator.SetBool(isMovingParam, isMoving);
    }
}
