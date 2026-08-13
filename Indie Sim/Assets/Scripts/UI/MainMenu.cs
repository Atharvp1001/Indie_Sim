using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    
    [Header("Scene Names")]
    [SerializeField] private string Scene1 = "CasualMode";
    [SerializeField] private string Scene2 = "HardcoreMode";

    public void LoadScene1 ()
    {
        // "Play" — funnels through StartNewRun() so GameSession actually
        // resets for the new run (it previously didn't at all).
        GameManager.Instance.StartNewRun();
    }

    public void LoadScene2 ()
    {
        GameManager.Instance.LoadScene(Scene2);
    }

}
