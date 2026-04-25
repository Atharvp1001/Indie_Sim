using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionHandler : MonoBehaviour
{
    [SerializeField] private string bossSceneName = "BossRoom1Scene";

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == bossSceneName)
        {
            SetupBossScene();
        }
        else
        {
            SetupRoguelikeScene();
        }
    }

    private void SetupBossScene()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        CinemachineCamera virtualCam = Object.FindFirstObjectByType<CinemachineCamera>();

        if (player != null && virtualCam != null)
        {
            virtualCam.Follow = player.transform;
            virtualCam.Target.TrackingTarget = player.transform;

            // ✅ Set camera distance
            CinemachinePositionComposer posComposer = virtualCam.GetComponent<CinemachinePositionComposer>();
            if (posComposer != null)
                posComposer.CameraDistance = 12f;

            Debug.Log("[SceneTransitionHandler] Camera assigned to player");
        }
        else
        {
            Debug.LogWarning("[SceneTransitionHandler] Player or Camera missing!");
        }
    }

    private void SetupRoguelikeScene()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        CinemachineCamera virtualCam = Object.FindFirstObjectByType<CinemachineCamera>();

        if (player != null && virtualCam != null)
        {
            virtualCam.Follow = player.transform;
            virtualCam.LookAt = player.transform;
            virtualCam.Target.TrackingTarget = player.transform;

            // ✅ Reset camera distance back to normal
            CinemachinePositionComposer posComposer = virtualCam.GetComponent<CinemachinePositionComposer>();
            if (posComposer != null)
                posComposer.CameraDistance = 8f; // ← whatever your normal value is

            Debug.Log("[SceneTransitionHandler] Camera assigned to player");
        }
        else
        {
            Debug.LogWarning("[SceneTransitionHandler] Player or Camera missing!");
        }
    }
}