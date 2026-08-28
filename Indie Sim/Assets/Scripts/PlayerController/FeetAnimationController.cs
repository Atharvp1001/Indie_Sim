using UnityEngine;

/// <summary>
/// Script-driven feet animation — no Animator involved.
///
/// Put this on the Feet child GameObject (must have a SpriteRenderer).
/// Assign the ordered list of frame sprites. The frames cycle/loop only while
/// the player is actually moving, and snap back to the idle frame when stopped.
///
/// Movement is read from the root player's Rigidbody2D velocity, matching
/// PlayerAnimationController.
///
/// Remove/disable the Animator component on this GameObject so it doesn't
/// fight the SpriteRenderer.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FeetAnimationController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Rigidbody2D to read movement from. Leave empty to auto-find on a parent.")]
    [SerializeField] private Rigidbody2D bodyRigidbody;

    [Header("Frames")]
    [Tooltip("Walk-cycle sprites in play order.")]
    [SerializeField] private Sprite[] frames;

    [Tooltip("Playback speed in frames per second.")]
    [SerializeField] private float framesPerSecond = 12f;

    [Tooltip("Frame index shown when the player is standing still.")]
    [SerializeField] private int idleFrameIndex = 0;

    [Tooltip("Restart the cycle from frame 0 each time the player starts moving. " +
             "If false, it resumes from wherever it left off.")]
    [SerializeField] private bool restartOnMove = true;

    [Header("Movement Detection")]
    [Tooltip("Speed (units/sec) above which the feet are considered moving.")]
    [SerializeField] private float moveThreshold = 0.1f;

    private SpriteRenderer spriteRenderer;
    private int currentFrame;
    private float frameTimer;
    private bool wasMoving;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (bodyRigidbody == null)
            bodyRigidbody = GetComponentInParent<Rigidbody2D>();

        if (bodyRigidbody == null)
            Debug.LogError("[FeetAnimationController] No Rigidbody2D found on this object or its parents.");
    }

    private void OnEnable()
    {
        wasMoving = false;
        ShowIdleFrame();
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        float speed = bodyRigidbody != null ? bodyRigidbody.linearVelocity.magnitude : 0f;
        bool isMoving = speed > moveThreshold;

        if (isMoving)
        {
            if (!wasMoving && restartOnMove)
            {
                currentFrame = 0;
                frameTimer = 0f;
                spriteRenderer.sprite = frames[0];
            }

            Advance();
        }
        else if (wasMoving)
        {
            ShowIdleFrame();
        }

        wasMoving = isMoving;
    }

    private void Advance()
    {
        if (framesPerSecond <= 0f) return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / framesPerSecond;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            currentFrame = (currentFrame + 1) % frames.Length;
            spriteRenderer.sprite = frames[currentFrame];
        }
    }

    private void ShowIdleFrame()
    {
        currentFrame = Mathf.Clamp(idleFrameIndex, 0, Mathf.Max(0, (frames?.Length ?? 1) - 1));
        frameTimer = 0f;
        if (frames != null && frames.Length > 0)
            spriteRenderer.sprite = frames[currentFrame];
    }
}
