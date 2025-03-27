using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // Enemy prefab to spawn
    public float spawnRadius = 5f; // Radius within which enemies will spawn
    public float spawnInterval = 3f; // Time between spawns
    public int maxEnemies = 10; // Max number of enemies at a time

    private int currentEnemyCount = 0;

    void Start()
    {
        InvokeRepeating(nameof(SpawnEnemy), spawnInterval, spawnInterval);
    }

    void SpawnEnemy()
    {
        if (currentEnemyCount >= maxEnemies) return;

        // Get a random position within the circular area
        Vector2 randomPosition = GetRandomPosition();

        // Instantiate enemy
        GameObject enemy = Instantiate(enemyPrefab, randomPosition, Quaternion.identity);
        currentEnemyCount++;

        // Subscribe to enemy death event
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.OnDeath += EnemyDied;
        }
    }

    Vector2 GetRandomPosition()
    {
        // Get a random angle
        float angle = Random.Range(0f, 2f * Mathf.PI);

        // Get a random distance within the spawn radius
        float distance = Random.Range(0f, spawnRadius);

        // Convert polar coordinates to Cartesian coordinates
        float x = transform.position.x + Mathf.Cos(angle) * distance;
        float y = transform.position.y + Mathf.Sin(angle) * distance;

        return new Vector2(x, y);
    }

    void EnemyDied()
    {
        currentEnemyCount--;
    }
}
