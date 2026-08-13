using Unity.Cinemachine;
using UnityEngine;

public class CameraZoomOnSpeed : MonoBehaviour
{
    public static CameraZoomOnSpeed Instance;

    [Header("Player Settings")]
    public GameObject player;

    [Header("Zoom Settings")]
    public float minZoom = 5f;
    public float maxZoom = 12f;
    public float firingZoom = 8f;
    public float zoomDamp = 3f;

    CinemachineCamera vcam;
    CinemachinePositionComposer positionComposer;
    bool isFiring = false;

  

    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        vcam = GetComponent<CinemachineCamera>();

        if (vcam != null)
        {
            positionComposer = vcam.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
        }
        vcam = GetComponent<CinemachineCamera>();

        if (vcam == null)
        {
            Debug.LogError("CinemachineCamera not found on this GameObject!");
            return;
        }

        // Try different ways to get the position composer
        positionComposer = vcam.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;

        if (positionComposer == null)
        {
            Debug.LogError("CinemachinePositionComposer not found! Make sure your camera Body is set to 'Position Composer'");

            // Try to find any body component
            var bodyComponent = vcam.GetCinemachineComponent(CinemachineCore.Stage.Body);
            if (bodyComponent != null)
            {
                Debug.Log($"Found body component of type: {bodyComponent.GetType().Name}");
            }
            return;
        }

        Debug.Log($"Initial CameraDistance: {positionComposer.CameraDistance}");
    }

    void Update()
    {
        if (player == null || positionComposer == null) return;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        float speed = rb.linearVelocity.magnitude;
        float targetZoom = GetTargetZoom(speed, isFiring);

        // Try to modify the camera distance
        float currentDistance = positionComposer.CameraDistance;
        float newDistance = Mathf.Lerp(currentDistance, targetZoom, Time.deltaTime * zoomDamp);

        positionComposer.CameraDistance = newDistance;

        // Debug output to see if values are changing
        if (Time.frameCount % 60 == 0) // Print once per second
        {
           // Debug.Log($"Speed: {speed:F2}, Target: {targetZoom:F2}, Current: {currentDistance:F2}, New: {newDistance:F2}, Firing: {isFiring}");
        }
    }

    public void StartFiring()
    {
        isFiring = true;
      //  Debug.Log("Started firing");
    }

    public void StopFiring()
    {
        isFiring = false;
       // Debug.Log("Stopped firing");
    }

    float GetTargetZoom(float speed, bool firing)
    {
        if (firing)
        {
            return firingZoom;
        }
        else if (speed > 0.1f)
        {
            return maxZoom;
        }
        else
        {
            return minZoom;
        }
    }
}
