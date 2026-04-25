using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    
    [Header("Scene Names")]
    [SerializeField] private string Scene1 = "CasualMode";
    [SerializeField] private string Scene2 = "HardcoreMode";

    public void LoadScene1 ()
    {
        GameManager.Instance.LoadScene(Scene1);
    }

    public void LoadScene2 ()
    {
        GameManager.Instance.LoadScene(Scene2);
    }

}
