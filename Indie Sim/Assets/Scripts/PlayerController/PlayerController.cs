using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseMoveSpeed = 5f; // Your default speed without upgrades

    [Header("References")]
    public FixedJoystick joystick; // Drag your joystick here in inspector

    private Rigidbody2D rb;
    private float currentMoveSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentMoveSpeed = baseMoveSpeed; // Initialize current speed
    }

    /// <summary>
    /// Set player movement speed
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        // If you have a speed variable, update it here
        // Example: if your speed variable is called 'moveSpeed'
        currentMoveSpeed = newSpeed;

        Debug.Log($"[PlayerMovement] Speed updated to: {newSpeed}");
    }


    void FixedUpdate()
    {
       

        
        rb.linearVelocity = new Vector2(joystick.Horizontal * currentMoveSpeed, joystick.Vertical * currentMoveSpeed);
        //Debug.Log("Current speed = "+ currentMoveSpeed);
    }

   
}
