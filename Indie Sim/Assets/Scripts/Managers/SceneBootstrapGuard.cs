using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Place in every non-Boot scene so pressing Play directly on that scene still
/// gets the [Persistent] root (GameSession, GameManager, etc.) instead of NPEing
/// on GameSession.Instance. If Boot has already run this session, this is a no-op.
/// </summary>
public class SceneBootstrapGuard : MonoBehaviour
{
    private void Awake()
    {
        if (GameSession.Instance == null)
        {
            SceneManager.LoadScene("Boot", LoadSceneMode.Additive);
            SceneManager.UnloadSceneAsync("Boot");
        }
    }
}
