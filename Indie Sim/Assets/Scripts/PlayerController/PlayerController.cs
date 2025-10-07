using UnityEngine;

[RequireComponent(typeof(Rigidbody2D),typeof(BoxCollider2D))]

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D Rigidbody2D;
    [SerializeField] private FixedJoystick joystick;

    [SerializeField] private float moveSpeed;

    [Header("Trail Particle System")]
    public ParticleSystem trailParticleSystem;

    void Start()
    {
        // Enable multi-touch for Android
        Input.multiTouchEnabled = true;

        // particle system setup
        // Just ensure it's set to world space simulation
        if (trailParticleSystem != null)
        {
            var main = trailParticleSystem.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }

        // Optional: Set maximum simultaneous touches
        // Input.simulateMouseWithTouches = false; // Prevents mouse simulation interfering
    }
    private void FixedUpdate()
    {
        Rigidbody2D.linearVelocity = new Vector2 (joystick.Horizontal*moveSpeed , joystick.Vertical * moveSpeed);
    }
}
