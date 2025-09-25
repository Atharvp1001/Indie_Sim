using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateEnemies : MonoBehaviour
{
    [Header("Activation Settings")]
    [SerializeField] private float activationRadius = 15f;
    [SerializeField] private float checkInterval = 0.3f; // Check every 0.3 seconds

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    // Static reference so enemies can easily access it
    public static ActivateEnemies Instance;

    // Set to track activated enemies (once activated, stays activated)
    private HashSet<GameObject> activatedEnemies = new HashSet<GameObject>();

    void Awake()
    {
        // Singleton pattern for easy access
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
        // Start checking for enemies periodically
        StartCoroutine(CheckForEnemies());
    }

    private IEnumerator CheckForEnemies()
    {
        while (true)
        {
            // Find all enemies in range
            Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(transform.position, activationRadius);

            foreach (Collider2D enemy in enemiesInRange)
            {
                // Check if it's an enemy and not already activated
                if (enemy.CompareTag("Enemy") && !activatedEnemies.Contains(enemy.gameObject))
                {
                    activatedEnemies.Add(enemy.gameObject);
                    Debug.Log($"Activated enemy: {enemy.name}");
                }
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    // Public method for enemies to check if they should be active
    public bool IsEnemyActivated(GameObject enemy)
    {
        return activatedEnemies.Contains(enemy);
    }

    // Optional: Method to get activation radius (for enemy spawners)
    public float GetActivationRadius()
    {
        return activationRadius;
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}
