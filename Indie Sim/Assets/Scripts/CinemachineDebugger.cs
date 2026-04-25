using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CinemachineDebugger : MonoBehaviour
{
    private CinemachineCamera cam;
    private float searchDuration = 5f;

    void Start()
    {
        cam = GetComponent<CinemachineCamera>();
        StartCoroutine(AttachToPlayerWithTimeout());
    }

    IEnumerator AttachToPlayerWithTimeout()
    {
        float timer = 0f;
        GameObject player = null;

        Debug.Log("[CameraDebug] Started searching for player...");

        while (timer < searchDuration)
        {
            player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                cam.Follow = player.transform;
                cam.LookAt = player.transform;

                Debug.Log("[CameraDebug] Player found and camera attached!");
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning("[CameraDebug] Player NOT found within 5 seconds!");
    }

    void OnDestroy()
    {
        Debug.LogError("[CameraDebug] CINEMACHINE DESTROYED: " + gameObject.name);

        // Optional: stack trace (very useful)
        Debug.LogError("[CameraDebug] Destroy stack trace:\n" + System.Environment.StackTrace);
    }

    void OnDisable()
    {
        Debug.LogWarning("[CameraDebug] Camera DISABLED (not destroyed): " + gameObject.name);
    }
}