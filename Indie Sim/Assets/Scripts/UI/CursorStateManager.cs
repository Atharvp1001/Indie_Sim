using UnityEngine;
using UnityEngine.SceneManagement;

public class CursorStateManager : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isMenu = scene.name == mainMenuSceneName;
        Cursor.visible = isMenu;
        Cursor.lockState = isMenu ? CursorLockMode.None : CursorLockMode.Locked;
    }
}