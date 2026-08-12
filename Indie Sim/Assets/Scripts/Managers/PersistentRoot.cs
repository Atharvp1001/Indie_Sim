using UnityEngine;

/// <summary>
/// Marks the "[Persistent]" root object in Boot.unity. Everything that must
/// survive scene loads is parented under this object rather than calling
/// DontDestroyOnLoad individually.
/// </summary>
public class PersistentRoot : MonoBehaviour
{
    private static PersistentRoot _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
