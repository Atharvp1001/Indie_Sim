using UnityEngine;

public class BloodSplatterEffect : MonoBehaviour
{
    [Header("Blood Splatter Prefab")]
    public ParticleSystem bloodSplatterPrefab; // Assign your blood particle system prefab

    [Header("Juice Settings")]
    [Range(0f, 90f)] public float maxRandomSpread = 30f; // How many degrees to wobble left/right
    /// <summary>
    /// Spawns blood splatter at enemy position, spraying away from player
    /// </summary>
    /// <param name="enemyPosition">Position where enemy was hit</param>
    /// <param name="playerPosition">Position of the player who shot</param>
    public void SpawnBloodSplatter(Vector3 enemyPosition, Vector3 playerPosition)
    {
        if (bloodSplatterPrefab == null)
        {
            Debug.LogError("Blood splatter prefab NOT ASSIGNED in BloodSplatterEffect!");
            return;
        }

        Debug.Log($"SpawnBloodSplatter called at position: {enemyPosition}");

        // Calculate direction from player to enemy (where blood should spray)
        Vector3 direction = (enemyPosition - playerPosition).normalized;

        // Calculate angle for 2D top-down (rotation around Z-axis)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Debug.Log($"Blood spray direction: {direction}, angle: {angle}");
        //allows the blood to splatter a little randmoly.(quality of life visual feature)
        float randomOffset = Random.Range(-maxRandomSpread, maxRandomSpread);
        float finalAngle = angle + randomOffset;

        // Create rotation for the blood splatter cone
        Quaternion rotation = Quaternion.Euler(0f, 0f, finalAngle);

        // Instantiate blood effect at enemy position with calculated rotation
        ParticleSystem blood = Instantiate(bloodSplatterPrefab, enemyPosition, rotation);

        if (blood == null)
        {
            Debug.LogError("Failed to instantiate blood particle system!");
            return;
        }

        Debug.Log($"Blood particle system instantiated: {blood.name}");

        // IMPORTANT: Make sure the particle system is active
        blood.gameObject.SetActive(true);

        // Get the main module to check settings
        var main = blood.main;
        Debug.Log($"Blood particle - Duration: {main.duration}, PlayOnAwake: {main.playOnAwake}, Looping: {main.loop}");
        Debug.Log($"Blood particle - MaxParticles: {main.maxParticles}, SimulationSpace: {main.simulationSpace}");

        // Check emission
        var emission = blood.emission;
        Debug.Log($"Blood particle - Emission enabled: {emission.enabled}, RateOverTime: {emission.rateOverTime.constant}, RateOverDistance: {emission.rateOverDistance.constant}");
        Debug.Log($"Blood particle - Burst count: {emission.burstCount}");

        // CRITICAL: Clear any existing particles and play fresh
        blood.Clear();
        blood.Play();

        Debug.Log($"Blood particle system played. IsPlaying: {blood.isPlaying}, IsEmitting: {blood.isEmitting}");

        // Destroy after particle lifetime ends
        float totalDuration = main.duration + main.startLifetime.constantMax;
        Destroy(blood.gameObject, totalDuration + 0.5f); // Add 0.5s buffer

        Debug.Log($"Blood particle will be destroyed in {totalDuration} seconds");
    }

    /// <summary>
    /// Alternative method if you already have the direction vector
    /// </summary>
    /// <param name="hitPosition">Position where hit occurred</param>
    /// <param name="hitDirection">Direction of the bullet/hit</param>
    public void SpawnBloodSplatter(Vector3 hitPosition, Vector2 hitDirection)
    {
        if (bloodSplatterPrefab == null)
        {
            Debug.LogError("Blood splatter prefab NOT ASSIGNED!");
            return;
        }

        Debug.Log($"SpawnBloodSplatter (direction version) called at: {hitPosition}");

        // Calculate angle from direction vector
        float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        ParticleSystem blood = Instantiate(bloodSplatterPrefab, hitPosition, rotation);

        if (blood == null)
        {
            Debug.LogError("Failed to instantiate blood particle system!");
            return;
        }

        // IMPORTANT: Ensure active and play
        blood.gameObject.SetActive(true);
        blood.Clear();
        blood.Play();

        var main = blood.main;
        float totalDuration = main.duration + main.startLifetime.constantMax;
        Destroy(blood.gameObject, totalDuration + 0.5f);

        Debug.Log($"Blood spawned. IsPlaying: {blood.isPlaying}");
    }
}
