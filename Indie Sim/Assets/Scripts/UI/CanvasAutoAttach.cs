using UnityEngine;
using System.Collections;

public class CanvasAutoAttach : MonoBehaviour
{
    private Camera cam;
    private GameObject player;

    void Start()
    {
        StartCoroutine(Attach());
    }

    IEnumerator Attach()
    {
        // Wait until player exists (handles pooling/delay)
        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return null;
        }

        // Get correct camera (player camera)
        cam = Camera.main;

        if (cam == null)
        {
            cam = FindFirstObjectByType<Camera>();
        }

        // If canvas uses camera (optional)
        Canvas canvas = GetComponent<Canvas>();

        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            canvas.worldCamera = cam;
        }

        Debug.Log("[Canvas] Attached to player + camera");
    }
}