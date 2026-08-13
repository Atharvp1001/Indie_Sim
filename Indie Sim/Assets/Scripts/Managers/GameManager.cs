using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Scene Names")]
    public string mainMenuScene = "Main Menu";
    public string gameScene = "RoguelikeScene";
    public string bossScene = "BossArena";

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneName)
    {
    Time.timeScale = 1f;
    UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
    // ───────────── SCENE LOADS ─────────────

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void LoadGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameScene);
    }

    public void LoadBoss()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(bossScene);
    }

    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}