using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sole owner of Cursor.visible/lockState. Lives under [Persistent] in Boot.unity.
/// Reads the SceneUIMode marker present in the newly loaded scene rather than
/// comparing scene names as strings.
/// </summary>
public class CursorController : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        Apply();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply();
    }

    private void Apply()
    {
        SceneUIMode uiMode = FindFirstObjectByType<SceneUIMode>();
        bool showCursor = uiMode != null && uiMode.ShowCursor;

        Cursor.visible = showCursor;
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Confined;
    }
}
