using UnityEngine;

public class AndroidTouchFix : MonoBehaviour
{
    void Awake()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        // Force enable multi-touch
        Input.multiTouchEnabled = true;
        
        // Prevent screen sleep
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        
        // Set target framerate
        Application.targetFrameRate = 60;
        
        // Disable mouse simulation conflicts
        Input.simulateMouseWithTouches = true;
        
        Debug.Log("Android touch settings applied");
        #endif
    }

    void Start()
    {
        // Verify settings
        Debug.Log($"Multi-touch enabled: {Input.multiTouchEnabled}");
        Debug.Log($"Touch supported: {Input.touchSupported}");
        Debug.Log($"Active Input Handling: {UnityEngine.InputSystem.InputSystem.settings}");
    }
}
