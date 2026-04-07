using UnityEngine;
using System.Collections.Generic;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement & Boids")]
    public float speed = 2.5f;
    public float rotationSpeed = 5f;
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float separationRadius = 1.2f;
    
    [Header("Weights")]
    [SerializeField] private float playerSeekWeight = 2f;
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;

    [Header("Optimization")]
    [SerializeField] private float gridCellSize = 5f;

    private Rigidbody2D rb;
    private Transform player;
    private bool isActivated = false;
    private Vector2 currentGridKey;

    // Spatial Partitioning Structure
    private static Dictionary<Vector2, List<EnemyMovement>> spatialGrid = new Dictionary<Vector2, List<EnemyMovement>>();

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        UpdateGridPosition();
    }

    void OnDisable() => RemoveFromGrid(currentGridKey);

    void FixedUpdate()
    {
        // Sync with your activation system
        if (ActivateEnemies.Instance != null)
            isActivated = ActivateEnemies.Instance.IsEnemyActivated(gameObject);

        if (!isActivated || player == null) return;

        UpdateGridPosition();
        
        Vector2 boidForce = CalculateSpatialBoids();
        Vector2 seekForce = ((Vector2)player.position - (Vector2)transform.position).normalized * playerSeekWeight;

        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, (boidForce + seekForce).normalized * speed, Time.fixedDeltaTime * 5f);
        RotateTowards(rb.linearVelocity);
    }

    private void UpdateGridPosition()
    {
        Vector2 newKey = new Vector2(Mathf.Floor(transform.position.x / gridCellSize), Mathf.Floor(transform.position.y / gridCellSize));
        
        if (newKey != currentGridKey)
        {
            RemoveFromGrid(currentGridKey);
            currentGridKey = newKey;
            if (!spatialGrid.ContainsKey(newKey)) spatialGrid[newKey] = new List<EnemyMovement>();
            spatialGrid[newKey].Add(this);
        }
    }

    private void RemoveFromGrid(Vector2 key)
    {
        if (spatialGrid.ContainsKey(key)) spatialGrid[key].Remove(this);
    }

    private Vector2 CalculateSpatialBoids()
    {
        Vector2 separation = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        Vector2 cohesion = Vector2.zero;
        int neighbors = 0;

        // Only check current cell + 8 surrounding cells
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2 checkKey = currentGridKey + new Vector2(x, y);
                if (spatialGrid.ContainsKey(checkKey))
                {
                    foreach (var other in spatialGrid[checkKey])
                    {
                        if (other == this) continue;
                        float d2 = (other.transform.position - transform.position).sqrMagnitude;
                        if (d2 < detectionRadius * detectionRadius)
                        {
                            neighbors++;
                            alignment += other.rb.linearVelocity.normalized;
                            cohesion += (Vector2)other.transform.position;
                            if (d2 < separationRadius * separationRadius)
                                separation += ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
                        }
                    }
                }
            }
        }

        if (neighbors == 0) return Vector2.zero;
        return (alignment.normalized * alignmentWeight + (cohesion / neighbors - (Vector2)transform.position).normalized * cohesionWeight + separation * separationWeight);
    }

    private void RotateTowards(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.1f) return;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, angle), rotationSpeed * Time.deltaTime);
    }

    public void ApplyKnockback(Vector2 force)
    {
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);
    }
}