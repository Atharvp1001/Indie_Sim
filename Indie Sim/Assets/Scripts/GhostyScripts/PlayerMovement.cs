using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector2 moveInput;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        // Correctly assigning to the class field
        moveInput = context.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        // Use velocity or MovePosition as per your desired physics feel
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
