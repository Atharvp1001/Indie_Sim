using UnityEngine;
using Unity.Cinemachine;

public class CinemachineCursorLead : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Cinemachine Camera to control")]
    public CinemachineCamera cinemachineCamera;

    [Tooltip("The player transform")]
    public Transform player;

    [Header("Cursor Lead Settings")]
    [Tooltip("How far the camera target shifts towards cursor (in world units)")]
    public float maxLeadDistance = 3f;

    [Tooltip("How much influence the cursor has (0 = only follow player, 1 = move fully toward cursor)")]
    [Range(0f, 1f)]
    public float cursorInfluence = 0.5f;

    [Tooltip("How smoothly the target moves")]
    public float smoothSpeed = 5f;

    private GameObject cameraTarget;
    private Vector3 targetPosition;
    private Camera mainCamera;

    void Start()
    {
        // Get main camera
        mainCamera = Camera.main;

        // Create a new GameObject to act as the camera's follow target
        cameraTarget = new GameObject("CameraFollowTarget");
        cameraTarget.transform.position = player.position;

        // Set the Cinemachine camera to track this new target
        if (cinemachineCamera != null)
        {
            cinemachineCamera.Follow = cameraTarget.transform;
            Debug.Log("Camera target created and assigned to Cinemachine!");
        }
        else
        {
            Debug.LogError("Cinemachine Camera not assigned!");
        }

        if (player == null)
        {
            Debug.LogError("Player not assigned!");
        }
    }

    void LateUpdate()
    {
        if (cameraTarget == null || player == null || mainCamera == null)
            return;

        // Calculate the target position (blend between player and cursor direction)
        CalculateTargetPosition();

        // Smoothly move the camera target to the calculated position
        cameraTarget.transform.position = Vector3.Lerp(
            cameraTarget.transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }

    void CalculateTargetPosition()
    {
        // Get mouse position in world space
        Vector3 mouseWorldPos = GetMouseWorldPosition();

        // Calculate direction from player to mouse
        Vector3 playerToMouse = mouseWorldPos - player.position;

        // For 2D (XY plane), ignore Z depth
        playerToMouse.z = 0f;

        // Clamp the distance
        Vector3 cursorOffset = Vector3.ClampMagnitude(playerToMouse, maxLeadDistance);

        // Apply cursor influence
        cursorOffset *= cursorInfluence;

        // Target position = player position + cursor offset
        targetPosition = player.position + cursorOffset;
    }

    Vector3 GetMouseWorldPosition()
    {
        // Convert mouse screen position to world position
        Vector3 mouseScreenPos = Input.mousePosition;

        // Set Z distance for ScreenToWorldPoint
        mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z - player.position.z);

        // Convert to world space
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);

        // Keep same Z as player (2D plane)
        mouseWorldPos.z = player.position.z;

        return mouseWorldPos;
    }

    void OnDestroy()
    {
        // Clean up the camera target when script is destroyed
        if (cameraTarget != null)
        {
            Destroy(cameraTarget);
        }
    }
}
