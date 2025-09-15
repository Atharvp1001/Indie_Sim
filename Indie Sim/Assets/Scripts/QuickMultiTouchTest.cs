using UnityEngine;

public class QuickMultiTouchTest : MonoBehaviour
{
    public FixedJoystick leftJoystick;
    public FixedJoystick rightJoystick;

    void Start()
    {
        Input.multiTouchEnabled = true;
    }

    void Update()
    {
        // Simple test - change background color when both are active
        bool leftActive = Mathf.Abs(leftJoystick.Horizontal) > 0.1f || Mathf.Abs(leftJoystick.Vertical) > 0.1f;
        bool rightActive = Mathf.Abs(rightJoystick.Horizontal) > 0.1f || Mathf.Abs(rightJoystick.Vertical) > 0.1f;

        if (leftActive && rightActive)
        {
            Camera.main.backgroundColor = Color.green; // Both working!
        }
        else if (leftActive)
        {
            Camera.main.backgroundColor = Color.blue; // Left only
        }
        else if (rightActive)
        {
            Camera.main.backgroundColor = Color.red; // Right only  
        }
        else
        {
            Camera.main.backgroundColor = Color.black; // Neither active
        }
    }
}
