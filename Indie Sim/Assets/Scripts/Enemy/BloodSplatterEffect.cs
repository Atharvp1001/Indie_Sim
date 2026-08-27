using UnityEngine;

public class BloodSplatterEffect : MonoBehaviour
{
    [Header("Blood Splatter Animation Prefabs")]
    public GameObject[] bloodSplatterPrefabs; // Array of blood animation GameObjects
    
    [Header("Lifetime Settings")]
    public float bloodLifetime = 2f; // How long the blood animation stays before being destroyed

    [Header("Blood Chunk Settings")]
    public Sprite bloodChunkSprite; // Single sprite that gets thrown
    [Range(3, 10)] public int chunkCount = 4; // How many chunks to spawn
    public float chunkLifetime = 0.5f; // How long chunks stay visible
    public float chunkSpeed = 5f; // How fast chunks fly away
    public float chunkSpreadAngle = 45f; // Spread angle for chunks
    
    [Header("Blood Chunk Sorting")]
    public string chunkSortingLayer = "Default";
    public int chunkSortingOrder = 0;

    [Header("Color Tint")]
    [Tooltip("Tints both the blood splatter prefab's particles and the blood chunks. White = untouched original colors.")]
    public Color bloodTintColor = Color.white;

    /// <summary>
    /// Spawns blood splatter at enemy position
    /// </summary>
    /// <param name="enemyPosition">Position where enemy was hit</param>
    /// <param name="playerPosition">Position of the player who shot</param>
    public void SpawnBloodSplatter(Vector3 enemyPosition, Vector3 playerPosition)
    {
        if (bloodSplatterPrefabs == null || bloodSplatterPrefabs.Length == 0)
        {
            Debug.LogError("Blood splatter prefabs NOT ASSIGNED in BloodSplatterEffect!");
            return;
        }

        // Pick a random blood splatter prefab from the array
        GameObject randomBloodPrefab = bloodSplatterPrefabs[Random.Range(0, bloodSplatterPrefabs.Length)];

        // Instantiate blood effect at enemy position
        GameObject blood = Instantiate(randomBloodPrefab, enemyPosition, Quaternion.identity);
        ApplyTint(blood);

        // Destroy after specified lifetime
        Destroy(blood, bloodLifetime);

        // Spawn blood chunks flying away from player
        if (bloodChunkSprite != null)
        {
            SpawnBloodChunks(enemyPosition, playerPosition);
        }
    }

    /// <summary>
    /// Alternative method if you already have the direction vector
    /// </summary>
    /// <param name="hitPosition">Position where hit occurred</param>
    /// <param name="hitDirection">Direction of the bullet/hit</param>
    public void SpawnBloodSplatter(Vector3 hitPosition, Vector2 hitDirection)
    {
        if (bloodSplatterPrefabs == null || bloodSplatterPrefabs.Length == 0)
        {
            Debug.LogError("Blood splatter prefabs NOT ASSIGNED!");
            return;
        }

        // Pick a random blood splatter prefab from the array
        GameObject randomBloodPrefab = bloodSplatterPrefabs[Random.Range(0, bloodSplatterPrefabs.Length)];

        // Instantiate blood effect
        GameObject blood = Instantiate(randomBloodPrefab, hitPosition, Quaternion.identity);
        ApplyTint(blood);

        // Destroy after specified lifetime
        Destroy(blood, bloodLifetime);

        // Spawn blood chunks in opposite direction
        if (bloodChunkSprite != null)
        {
            Vector3 oppositeDirection = -hitDirection;
            SpawnBloodChunksWithDirection(hitPosition, oppositeDirection);
        }
    }

    private void SpawnBloodChunks(Vector3 enemyPosition, Vector3 playerPosition)
    {
        // Calculate direction away from player (opposite of bullet direction)
        Vector3 awayFromPlayer = (enemyPosition - playerPosition).normalized;
        SpawnBloodChunksWithDirection(enemyPosition, awayFromPlayer);
    }

    private void SpawnBloodChunksWithDirection(Vector3 spawnPosition, Vector3 baseDirection)
    {
        for (int i = 0; i < chunkCount; i++)
        {
            // Create chunk GameObject
            GameObject chunk = new GameObject("BloodChunk");
            chunk.transform.position = spawnPosition;

            // Add sprite renderer
            SpriteRenderer renderer = chunk.AddComponent<SpriteRenderer>();
            renderer.sprite = bloodChunkSprite;
            renderer.sortingLayerName = chunkSortingLayer;
            renderer.sortingOrder = chunkSortingOrder;
            renderer.color = bloodTintColor;

            // Calculate spread direction
            float spreadOffset = Random.Range(-chunkSpreadAngle / 2f, chunkSpreadAngle / 2f);
            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
            float finalAngle = baseAngle + spreadOffset;
            
            Vector2 direction = new Vector2(
                Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                Mathf.Sin(finalAngle * Mathf.Deg2Rad)
            );

            // Add movement component
            BloodChunkMover mover = chunk.AddComponent<BloodChunkMover>();
            mover.Initialize(direction * chunkSpeed, chunkLifetime);
        }
    }

    /// <summary>
    /// Tints every particle system on the spawned blood prefab. Uses startColor.color
    /// (not .color, which would only tint the multiplier) so it works whether the
    /// particle's own gradient is white or already colored.
    /// </summary>
    private void ApplyTint(GameObject blood)
    {
        if (bloodTintColor == Color.white) return; // no-op, avoid touching original gradients needlessly

        foreach (ParticleSystem ps in blood.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule main = ps.main;
            main.startColor = bloodTintColor;
        }
    }
}

// Simple component to move blood chunks
public class BloodChunkMover : MonoBehaviour
{
    private Vector2 velocity;
    private float lifetime;

    public void Initialize(Vector2 initialVelocity, float life)
    {
        velocity = initialVelocity;
        lifetime = life;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += (Vector3)velocity * Time.deltaTime;
    }
}