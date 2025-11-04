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
    }

    void FixedUpdate()
    {
       

        
        rb.linearVelocity = new Vector2(joystick.Horizontal * baseMoveSpeed, joystick.Vertical * baseMoveSpeed);
        //Debug.Log("Current speed = "+ currentMoveSpeed);
    }

   
}
