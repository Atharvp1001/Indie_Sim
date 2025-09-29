using UnityEngine;
using UnityEngine.EventSystems;

// Add this to your existing Joystick class or create a new script
public class JoystickTouchFix : MonoBehaviour
{
    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Ensure the GraphicRaycaster settings are correct
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            UnityEngine.UI.GraphicRaycaster raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.ignoreReversedGraphics = false;
                raycaster.blockingObjects = UnityEngine.UI.GraphicRaycaster.BlockingObjects.None;
            }
        }
#endif
    }
}
