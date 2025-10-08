using UnityEngine;

public class SimplePlayerRotation : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private FixedJoystick joystick;
    [SerializeField] private Transform spriteToRotate;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 720f; // Degrees per second
    [SerializeField] private float deadZone = 0.1f;

    void Update()
    {
        // Get joystick input
        Vector2 input = new Vector2(joystick.Horizontal, joystick.Vertical);

        // Only rotate if moving beyond deadzone
        if (input.magnitude > deadZone)
        {
            // Calculate target angle
            float targetAngle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.AngleAxis(targetAngle + 90f, Vector3.forward);

            // Smoothly rotate towards target
            if (spriteToRotate == null) spriteToRotate = transform;
            spriteToRotate.rotation = Quaternion.RotateTowards(
                spriteToRotate.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}
