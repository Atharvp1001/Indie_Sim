using UnityEngine;

[RequireComponent(typeof(Rigidbody2D),typeof(BoxCollider2D))]

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D Rigidbody2D;
    [SerializeField] private FixedJoystick joystick;

    [SerializeField] private float moveSpeed;

    void Start()
    {
        // Enable multi-touch for Android
        Input.multiTouchEnabled = true;

        // Optional: Set maximum simultaneous touches
        // Input.simulateMouseWithTouches = false; // Prevents mouse simulation interfering
    }
    private void FixedUpdate()
    {
        Rigidbody2D.linearVelocity = new Vector2 (joystick.Horizontal*moveSpeed , joystick.Vertical * moveSpeed);
    }
}
