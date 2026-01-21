using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateEnemies : MonoBehaviour
{
    [Header("Activation Settings")]
    [SerializeField] private float activationRadius = 15f;
    [SerializeField] private float checkInterval = 0.3f;
    [SerializeField] private int maxActiveEnemies = 15; // NEW: Limit on active enemies
    
    [Header("Pathfinding Settings")]
    [SerializeField] private int maxPathfindingEnemies = 10; // Max enemies that can pathfind simultaneously
    [SerializeField] private float pathfindingPriorityRadius = 8f; // Closer enemies get priority

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    // Static reference
    public static ActivateEnemies Instance;

    // Enemy tracking
    private HashSet<GameObject> activatedEnemies = new HashSet<GameObject>();
    private List<GameObject> enemiesInRange = new List<GameObject>(); // For priority sorting
    private HashSet<EnemyMovement> pathfindingEnemies = new HashSet<EnemyMovement>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        StartCoroutine(CheckForEnemies());
    }

    private IEnumerator CheckForEnemies()
    {
        while (true)
        {
            UpdateEnemyActivation();
            yield return new WaitForSeconds(checkInterval);
        }
    }

    private void UpdateEnemyActivation()
    {
        // Find all enemies in range
        Collider2D[] enemiesInRangeColliders = Physics2D.OverlapCircleAll(transform.position, activationRadius);
        
        // Clear and rebuild the in-range list
        enemiesInRange.Clear();
        
        foreach (Collider2D enemy in enemiesInRangeColliders)
        {
            if (enemy.CompareTag("Enemy"))
            {
                enemiesInRange.Add(enemy.gameObject);
            }
        }
        
        // Sort by distance (closest first for priority)
        enemiesInRange.Sort((a, b) => 
        {
            float distA = Vector2.Distance(transform.position, a.transform.position);
            float distB = Vector2.Distance(transform.position, b.transform.position);
            return distA.CompareTo(distB);
        });
        
        // Activate up to maxActiveEnemies, prioritizing closest
        int activatedCount = 0;
        HashSet<GameObject> shouldBeActive = new HashSet<GameObject>();
        
        foreach (GameObject enemy in enemiesInRange)
        {
            if (activatedCount < maxActiveEnemies)
            {
                shouldBeActive.Add(enemy);
                
                if (!activatedEnemies.Contains(enemy))
                {
                    activatedEnemies.Add(enemy);
                    Debug.Log($"Activated enemy: {enemy.name} ({activatedCount + 1}/{maxActiveEnemies})");
                }
                
                activatedCount++;
            }
        }
        
        // Deactivate enemies that are no longer in priority range
        List<GameObject> toDeactivate = new List<GameObject>();
        foreach (GameObject enemy in activatedEnemies)
        {
            if (!shouldBeActive.Contains(enemy))
            {
                toDeactivate.Add(enemy);
            }
        }
        
        foreach (GameObject enemy in toDeactivate)
        {
            activatedEnemies.Remove(enemy);
            
            // Also disable pathfinding if they had it
            EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();
            if (enemyMovement != null && enemyMovement.IsPathfinding())
            {
                enemyMovement.DisablePathfinding();
            }
            
            Debug.Log($"Deactivated enemy: {enemy.name}");
        }
        
        // Update pathfinding assignments
        UpdatePathfindingAssignments();
    }

    private void UpdatePathfindingAssignments()
    {
        // Clean up destroyed enemies
        pathfindingEnemies.RemoveWhere(e => e == null);
        
        // Get all active enemy movements
        List<EnemyMovement> activeEnemyMovements = new List<EnemyMovement>();
        
        foreach (GameObject enemy in activatedEnemies)
        {
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                activeEnemyMovements.Add(movement);
            }
        }
        
        // Sort by distance for pathfinding priority
        activeEnemyMovements.Sort((a, b) => 
        {
            float distA = Vector2.Distance(transform.position, a.transform.position);
            float distB = Vector2.Distance(transform.position, b.transform.position);
            return distA.CompareTo(distB);
        });
        
        // Assign pathfinding to closest enemies that need it (leaders or solo enemies)
        int pathfindingCount = pathfindingEnemies.Count;
        
        foreach (EnemyMovement enemy in activeEnemyMovements)
        {
            // Skip if at max capacity
            if (pathfindingCount >= maxPathfindingEnemies)
                break;
            
            // Only give pathfinding to leaders or enemies without a flock
            if (enemy.IsPathfinding())
            {
                // Already pathfinding
                continue;
            }
            
            // Check if enemy is within priority radius and is a leader or solo
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance <= pathfindingPriorityRadius)
            {
                // Try to enable pathfinding
                TryEnablePathfinding(enemy);
                pathfindingCount = pathfindingEnemies.Count;
            }
        }
        
        // Disable pathfinding for enemies outside priority radius
        List<EnemyMovement> toDisable = new List<EnemyMovement>();
        foreach (EnemyMovement enemy in pathfindingEnemies)
        {
            if (enemy != null)
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance > pathfindingPriorityRadius)
                {
                    toDisable.Add(enemy);
                }
            }
        }
        
        foreach (EnemyMovement enemy in toDisable)
        {
            enemy.DisablePathfinding();
        }
    }

    public bool IsEnemyActivated(GameObject enemy)
    {
        return activatedEnemies.Contains(enemy);
    }

    public bool TryEnablePathfinding(EnemyMovement enemy)
    {
        if (enemy == null) return false;
        
        // Check if already pathfinding
        if (pathfindingEnemies.Contains(enemy))
            return true;
        
        // Check if we have capacity
        if (pathfindingEnemies.Count >= maxPathfindingEnemies)
        {
            Debug.Log($"Cannot enable pathfinding for {enemy.gameObject.name}: Max capacity reached ({maxPathfindingEnemies})");
            return false;
        }
        
        // Check if enemy is activated
        if (!activatedEnemies.Contains(enemy.gameObject))
        {
            Debug.Log($"Cannot enable pathfinding for {enemy.gameObject.name}: Enemy not activated");
            return false;
        }
        
        // Enable pathfinding
        pathfindingEnemies.Add(enemy);
        enemy.EnablePathfinding();
        Debug.Log($"Enabled pathfinding for {enemy.gameObject.name} ({pathfindingEnemies.Count}/{maxPathfindingEnemies})");
        return true;
    }

    public void UnregisterPathfindingEnemy(EnemyMovement enemy)
    {
        if (pathfindingEnemies.Remove(enemy))
        {
            Debug.Log($"Unregistered pathfinding for {enemy.gameObject.name} ({pathfindingEnemies.Count}/{maxPathfindingEnemies})");
        }
    }

    public float GetActivationRadius()
    {
        return activationRadius;
    }

    public int GetActiveEnemyCount()
    {
        return activatedEnemies.Count;
    }

    public int GetPathfindingEnemyCount()
    {
        return pathfindingEnemies.Count;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Activation radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
        
        // Pathfinding priority radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pathfindingPriorityRadius);
        
        // Draw lines to pathfinding enemies
        if (Application.isPlaying && pathfindingEnemies != null)
        {
            Gizmos.color = Color.magenta;
            foreach (EnemyMovement enemy in pathfindingEnemies)
            {
                if (enemy != null)
                {
                    Gizmos.DrawLine(transform.position, enemy.transform.position);
                }
            }
        }
    }

    // Debug UI
    void OnGUI()
    {
        if (showDebugGizmos)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 16;
            style.normal.textColor = Color.white;
            
            GUI.Label(new Rect(10, 10, 300, 30), $"Active Enemies: {activatedEnemies.Count}/{maxActiveEnemies}", style);
            GUI.Label(new Rect(10, 30, 300, 30), $"Pathfinding Enemies: {pathfindingEnemies.Count}/{maxPathfindingEnemies}", style);
        }
    }
}