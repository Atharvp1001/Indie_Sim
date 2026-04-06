using UnityEngine;

public class ManualGoreCollisions : MonoBehaviour
{
    private ChunkedGorePainter painter;
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
        painter = Object.FindAnyObjectByType<ChunkedGorePainter>();

        // FORCE the particle system to always simulate so far-away kills work
        var main = ps.main;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
    }

    void LateUpdate()
    {
        if (painter == null) return;

        int numParticlesAlive = ps.GetParticles(particles);
        bool simulationIsWorld = ps.main.simulationSpace == ParticleSystemSimulationSpace.World;

        for (int i = 0; i < numParticlesAlive; i++)
        {
            // NEW LOGIC: Instead of waiting for death, we check if it's "Hitting the floor"
            // Or, if you prefer the 'Burst' look, we paint as soon as they slow down (Drag)
            
            if (particles[i].remainingLifetime <= Time.deltaTime)
            {
                Vector3 pos = simulationIsWorld ? particles[i].position : transform.TransformPoint(particles[i].position);
                pos.z = 0;
                
                painter.PaintSplat(pos);
                
                // To prevent the "double stamp" or flickering, 
                // we kill the particle immediately after painting
                particles[i].remainingLifetime = -1f; 
            }
        }
        
        // Apply the "immediate death" back to the system to remove the gap
        ps.SetParticles(particles, numParticlesAlive);
    }
}