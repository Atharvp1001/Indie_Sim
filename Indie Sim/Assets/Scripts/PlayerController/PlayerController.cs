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
        // Calculate current speed with upgrades
        UpdateMoveSpeed();

        // Apply movement
        rb.linearVelocity = new Vector2(joystick.Horizontal * currentMoveSpeed, joystick.Vertical * currentMoveSpeed);
        //Debug.Log("Current speed = "+ currentMoveSpeed);
    }

    void UpdateMoveSpeed()
    {
        // Base speed + bonus from UpgradeManager
        if (UpgradeManager.Instance != null)
        {
            currentMoveSpeed = baseMoveSpeed + UpgradeManager.Instance.speedBonus;
        }
        else
        {
            currentMoveSpeed = baseMoveSpeed;
        }
    }
}
