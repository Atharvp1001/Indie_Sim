using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sole owner of Cursor.visible/lockState. Lives under [Persistent] in Boot.unity.
/// Reads the SceneUIMode marker present in the newly loaded scene rather than
/// comparing scene names as strings.
/// </summary>
public class CursorController : MonoBehaviour
{
    public static CursorController Instance { get; private set; }

    // Set by in-scene UI (e.g. DemoCompleteScreen, pause menus) that need the
    // cursor visible without a scene change to trigger HandleSceneLoaded.
    // Cleared automatically on the next scene load.
    private bool _overrideShowCursor;

    private void Awake()
    {
        Instance = this;
    }

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
        _overrideShowCursor = false;
        Apply();
    }

    /// <summary>
    /// For in-scene UI (paused overlays, end screens) that need the cursor
    /// visible without a scene load. Overrides SceneUIMode until the next
    /// scene load, which resets it automatically.
    /// </summary>
    public void SetCursorOverride(bool showCursor)
    {
        _overrideShowCursor = showCursor;
        Apply();
    }

    private void Apply()
    {
        SceneUIMode uiMode = FindFirstObjectByType<SceneUIMode>();
        bool showCursor = _overrideShowCursor || (uiMode != null && uiMode.ShowCursor);

        Cursor.visible = showCursor;
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Confined;
    }
}
