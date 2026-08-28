using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lives in Boot.unity, outside the [Persistent] root (it must not survive the
/// scene load it triggers). Only advances to the next scene when Boot was
/// loaded as the active scene (a real cold boot) — not when SceneBootstrapGuard
/// pulled Boot in additively just to spawn the persistent objects.
/// </summary>
public class BootLoader : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Main menu";

    private void Start()
    {
        if (SceneManager.GetActiveScene() == gameObject.scene)
        {
            SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        }
    }
}
