using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionHandler : MonoBehaviour
{
    [SerializeField] private string bossSceneName = "BossLevel";

    private Camera playerCam;

    public void HandleBossSceneLoad()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (scene.name == bossSceneName)
        {
            SetupBossScene();
        }
        else
        {
            // ✅ Returning to roguelike scene — reattach camera to player
            SetupRoguelikeScene();
        }
    }

    private void SetupBossScene()
    {
        // Find and detach player camera
        playerCam = null;
        foreach (Camera cam in GetComponentsInChildren<Camera>())
        {
            if (cam.CompareTag("MainCamera"))
            {
                playerCam = cam;
                break;
            }
        }

        if (playerCam != null)
        {
            playerCam.transform.SetParent(null);
            playerCam.transform.position = new Vector3(0f, 0f, -11.6f);
            Debug.Log("[SceneTransitionHandler] Camera detached and fixed for boss scene");
        }
        else
            Debug.LogWarning("[SceneTransitionHandler] No MainCamera found in player children!");

        // Reassign camera to shooter
        PlayerConeShooter shooter = GetComponent<PlayerConeShooter>();
        if (shooter != null) shooter.SetCamera(Camera.main);

        // Move player to spawn point
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawnPoint");
        transform.position = spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;

        Debug.Log("[SceneTransitionHandler] Boss scene setup complete");
    }

    private void SetupRoguelikeScene()
    {
        // ✅ Reattach camera back to player
        if (playerCam != null)
        {
            playerCam.transform.SetParent(transform);
            playerCam.transform.localPosition = new Vector3(0f, 0f, -11.6f);
            Debug.Log("[SceneTransitionHandler] Camera reattached to player");
        }
        else
        {
            // Camera was never detached — find it as child
            foreach (Camera cam in GetComponentsInChildren<Camera>())
            {
                if (cam.CompareTag("MainCamera"))
                {
                    playerCam = cam;
                    break;
                }
            }
            Debug.Log("[SceneTransitionHandler] Camera already attached to player");
        }

        // Reassign camera to shooter
        PlayerConeShooter shooter = GetComponent<PlayerConeShooter>();
        if (shooter != null) shooter.SetCamera(Camera.main);

        // Move player to spawn point
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawnPoint");
        transform.position = spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;

        Debug.Log("[SceneTransitionHandler] Roguelike scene setup complete");
    }

    public void HandleRoguelikeSceneLoad()
    {
        SetupRoguelikeScene();
    }
}