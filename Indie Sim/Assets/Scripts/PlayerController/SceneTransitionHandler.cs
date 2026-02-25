using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this to the Player GameObject.
/// Handles all setup needed after transitioning to the boss scene.
/// </summary>
public class SceneTransitionHandler : MonoBehaviour
{
    public void HandleBossSceneLoad()
    {
        SceneManager.sceneLoaded += OnBossSceneLoaded;
    }

    private void OnBossSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnBossSceneLoaded;

        // ✅ Find the main camera specifically by tag, not just any camera child
        Camera playerCam = null;
        Camera[] allCams = GetComponentsInChildren<Camera>();

        foreach (Camera cam in allCams)
        {
            if (cam.CompareTag("MainCamera"))
            {
                playerCam = cam;
                break;
            }
        }

        if (playerCam != null)
        {
            // Detach from player so it stops following
            playerCam.transform.SetParent(null);

            // Fix to boss room position — adjust X,Y to your boss room centre
            playerCam.transform.position = new Vector3(0f, 0f, -15.8f);

            Debug.Log("[SceneTransitionHandler] Main camera detached and fixed for boss scene");
        }
        else
        {
            Debug.LogWarning("[SceneTransitionHandler] No MainCamera found in player children!");
        }

        // Reassign camera to PlayerConeShooter
        PlayerConeShooter shooter = GetComponent<PlayerConeShooter>();
        if (shooter != null)
            shooter.SetCamera(Camera.main);

        // Move player to spawn point
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawnPoint");
        transform.position = spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;

        Debug.Log("[SceneTransitionHandler] Boss scene setup complete");
    }


}
