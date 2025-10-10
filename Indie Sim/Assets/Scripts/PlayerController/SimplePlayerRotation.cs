using UnityEngine;

public class SimplePlayerRotation : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private FixedJoystick movementJoystick;
    [SerializeField] private PlayerConeShooter playerShooter; // Direct reference to your shooter
    [SerializeField] private PlayerAutoAimShooter autoAimShooter; // Reference to auto-aim shooter
    [SerializeField] private Transform spriteToRotate;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float deadZone = 0.1f;

    void Start()
    {
        // Auto-find PlayerConeShooter if not assigned
        if (playerShooter == null)
        {
            playerShooter = GetComponent<PlayerConeShooter>();
        }

        // Auto-find PlayerAutoAimShooter if not assigned
        if (autoAimShooter == null)
        {
            autoAimShooter = GetComponent<PlayerAutoAimShooter>();
        }
    }

    void Update()
    {
        Vector2 rotationInput = GetPriorityRotationInput();

        if (rotationInput.magnitude > deadZone)
        {
            RotateTowards(rotationInput);
        }
    }

    private Vector2 GetPriorityRotationInput()
    {
        // PRIORITY 1: Auto-aim direction (if auto-aim is enabled and has a target)
        if (autoAimShooter != null && autoAimShooter.isActiveAndEnabled)
        {
            Vector2 autoAimDirection = autoAimShooter.GetAutoAimDirection();
            if (autoAimDirection.magnitude > deadZone)
            {
                return autoAimDirection; // Highest priority - rotate towards auto-aim target
            }
        }

        // PRIORITY 2: Manual shooting direction (if actively shooting)
        if (playerShooter != null)
        {
            Vector2 shootingDirection = playerShooter.GetShootingDirection();
            if (shootingDirection.magnitude > deadZone)
            {
                return shootingDirection; // Manual shooting takes priority over movement
            }
        }

        // PRIORITY 3: Movement direction (fallback)
        if (movementJoystick != null)
        {
            Vector2 movementInput = new Vector2(movementJoystick.Horizontal, movementJoystick.Vertical);
            if (movementInput.magnitude > deadZone)
            {
                return movementInput;
            }
        }

        return Vector2.zero;
    }

    private void RotateTowards(Vector2 input)
    {
        float targetAngle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.AngleAxis(targetAngle + 135f, Vector3.forward);

        if (spriteToRotate == null) spriteToRotate = transform;
        spriteToRotate.rotation = Quaternion.RotateTowards(
            spriteToRotate.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}
