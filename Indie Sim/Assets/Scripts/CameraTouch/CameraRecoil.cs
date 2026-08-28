using UnityEngine;
using Unity.Cinemachine;

public class CameraRecoil : MonoBehaviour
{
    public static CameraRecoil Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CinemachineCamera cinemachineCamera;

    [Header("Recoil Settings")]
    [Tooltip("How far the camera kicks back (in world units)")]
    public float recoilStrength = 0.15f;

    [Tooltip("How long the recoil kick lasts before returning (80-120ms recommended)")]
    public float recoilDuration = 0.1f; // 100ms

    [Tooltip("Strength of the spring that pulls camera back")]
    public float returnStrength = 15f;

    [Tooltip("Dampening to smooth the return motion")]
    public float dampening = 5f;

    // Private variables
    private CinemachineFollow followComponent;
    private Vector3 recoilOffset = Vector3.zero;
    private Vector3 recoilVelocity = Vector3.zero;
    private Vector3 targetRecoilOffset = Vector3.zero;
    private Vector3 baseFollowOffset;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Get Cinemachine camera if not assigned
        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindObjectOfType<CinemachineCamera>();
        }

        if (cinemachineCamera != null)
        {
            followComponent = cinemachineCamera.GetComponent<CinemachineFollow>();

            if (followComponent != null)
            {
                // Store the base offset to apply recoil on top of it
                baseFollowOffset = followComponent.FollowOffset;
                Debug.Log("CameraRecoil initialized successfully");
            }
            else
            {
                Debug.LogError("CameraRecoil: No CinemachineFollow found on camera!");
            }
        }
        else
        {
            Debug.LogError("CameraRecoil: No CinemachineCamera found!");
        }
    }

    private void LateUpdate()
    {
        if (followComponent == null) return;

        // Calculate spring physics for smooth return
        float deltaTime = Time.deltaTime;

        // Spring acceleration (pulls back to zero)
        Vector3 acceleration = -returnStrength * recoilOffset - dampening * recoilVelocity;

        // Update velocity and position
        recoilVelocity += acceleration * deltaTime;
        recoilOffset += recoilVelocity * deltaTime;

        // Apply recoil offset to camera
        followComponent.FollowOffset = baseFollowOffset + recoilOffset;
    }

    /// <summary>
    /// Applies camera recoil in the OPPOSITE direction of shooting
    /// </summary>
    /// <param name="shootDirection">The direction the player is shooting (normalized)</param>
    public void ApplyRecoil(Vector2 shootDirection)
    {
        if (followComponent == null) return;

        // Kick camera in OPPOSITE direction of shooting
        Vector2 recoilDirection = -shootDirection.normalized;

        // For 2D top-down (XY plane), recoil affects X and Y offset
        Vector3 recoilKick = new Vector3(
            recoilDirection.x * recoilStrength,
            recoilDirection.y * recoilStrength,
            0f
        );

        // Add to current recoil (allows stacking for rapid fire)
        recoilOffset += recoilKick;

        Debug.Log($"Camera recoil applied: Shoot dir = {shootDirection}, Recoil = {recoilDirection}");
    }

    /// <summary>
    /// Applies camera recoil with custom strength
    /// </summary>
    public void ApplyRecoil(Vector2 shootDirection, float customStrength)
    {
        if (followComponent == null) return;

        Vector2 recoilDirection = -shootDirection.normalized;

        Vector3 recoilKick = new Vector3(
            recoilDirection.x * customStrength,
            recoilDirection.y * customStrength,
            0f
        );

        recoilOffset += recoilKick;
    }

    /// <summary>
    /// Resets recoil instantly (useful for cutscenes or teleports)
    /// </summary>
    public void ResetRecoil()
    {
        recoilOffset = Vector3.zero;
        recoilVelocity = Vector3.zero;

        if (followComponent != null)
        {
            followComponent.FollowOffset = baseFollowOffset;
        }
    }

    /// <summary>
    /// Updates the base offset (call this if you change camera offset elsewhere)
    /// </summary>
    public void UpdateBaseOffset(Vector3 newBaseOffset)
    {
        baseFollowOffset = newBaseOffset;
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || followComponent == null) return;

        // Draw current recoil offset
        Gizmos.color = Color.red;
        Vector3 cameraPos = cinemachineCamera.transform.position;
        Gizmos.DrawLine(cameraPos, cameraPos + recoilOffset * 10f); // Scaled for visibility
        Gizmos.DrawWireSphere(cameraPos + recoilOffset * 10f, 0.2f);
    }
}
