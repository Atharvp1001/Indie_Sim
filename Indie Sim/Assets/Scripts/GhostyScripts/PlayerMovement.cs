using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Vector2 moveInput;
    private Vector2 lastFacingDirection = Vector2.right; // Default to facing right
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Called when joystick is moved
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        if (moveInput != Vector2.zero)
        {
            lastFacingDirection = moveInput.normalized;
        }
    }

    // Called when attack button is pressed
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            string direction = GetCurrentDirection();
            Debug.Log("Attacking " + direction); // This confirms attack input is working
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    private string GetCurrentDirection()
    {
        return GetEightDirection(lastFacingDirection);
    }

    private string GetEightDirection(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360;

        string[] directions = new string[]
        {
            "Right",       // 0°
            "Up-Right",    // 45°
            "Up",          // 90°
            "Up-Left",     // 135°
            "Left",        // 180°
            "Down-Left",   // 225°
            "Down",        // 270°
            "Down-Right"   // 315°
        };

        int index = Mathf.RoundToInt(angle / 45f) % 8;
        return directions[index];
    }
}
