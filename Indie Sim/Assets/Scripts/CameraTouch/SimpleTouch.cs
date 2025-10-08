using UnityEngine;

public class SimpleTouch : MonoBehaviour
{
    void Update()
    {
        // Test basic mouse/touch input
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Debug.Log($"Touch/Mouse detected at: {mousePos}");
        }

        // Test touch input specifically  
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Debug.Log($"Touch detected: {touch.position}, Phase: {touch.phase}");
        }
    }
}
