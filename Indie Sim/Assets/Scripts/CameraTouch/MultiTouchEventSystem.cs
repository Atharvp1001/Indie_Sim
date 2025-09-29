using UnityEngine;
using UnityEngine.EventSystems;

public class MultiTouchEventSystem : MonoBehaviour
{
    void Start()
    {
        // Ensure the EventSystem supports multi-touch
        EventSystem eventSystem = GetComponent<EventSystem>();
        if (eventSystem == null)
        {
            // Use the new non-deprecated method
            eventSystem = FindFirstObjectByType<EventSystem>();
        }

        if (eventSystem != null)
        {
            // StandaloneInputModule now works for all platforms automatically
            // No need to force module active anymore
            StandaloneInputModule inputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (inputModule != null)
            {
                // The module is automatically active, just ensure it exists
                Debug.Log("StandaloneInputModule found and active");
            }
        }

        // Enable multi-touch
        Input.multiTouchEnabled = true;
    }
}
