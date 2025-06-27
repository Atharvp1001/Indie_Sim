using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float MoveSpeed;

    private Rigidbody2D rb;
    private Vector2 MovementValue;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = MovementValue * MoveSpeed;
    }

    private void OnMove(InputValue value)
    {
        MovementValue = value.Get<Vector2>();
    }


}
