using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateEnemies : MonoBehaviour
{
    [Header("Activation Settings")]
    [SerializeField] private float activationRadius = 15f;
    [SerializeField] private float checkInterval = 0.3f;
    [SerializeField] private int maxActiveEnemies = 15;

    [Header("Pathfinding")]
    [Tooltip("Enable JumpFlood pathfinding assignment. Uncheck to activate enemies by distance only — no pathfinding slots will be assigned.")]
    [SerializeField] private bool usePathfinding = true;
    [SerializeField] private int maxPathfindingEnemies = 10;
    [SerializeField] private float pathfindingPriorityRadius = 8f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    public static ActivateEnemies Instance;

    private HashSet<GameObject>     activatedEnemies  = new HashSet<GameObject>();
    private List<GameObject>        enemiesInRange    = new List<GameObject>();
    private HashSet<EnemyMovement>  pathfindingEnemies = new HashSet<EnemyMovement>();

    // -------------------------------------------------------------------------
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        StartCoroutine(CheckForEnemies());
    }

    // -------------------------------------------------------------------------
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
        // Remove destroyed references
        activatedEnemies.RemoveWhere(e => e == null);

        // Find all enemies in activation radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, activationRadius);
        enemiesInRange.Clear();
        foreach (Collider2D hit in hits)
            if (hit != null && hit.CompareTag("Enemy"))
                enemiesInRange.Add(hit.gameObject);

        // Nothing nearby — clear everything
        if (enemiesInRange.Count == 0)
        {
            if (activatedEnemies.Count > 0 || pathfindingEnemies.Count > 0)
            {
                Debug.Log("ActivateEnemies: No enemies in range, clearing all references");
                activatedEnemies.Clear();
                pathfindingEnemies.Clear();
            }
            return;
        }

        // Sort closest-first for priority
        enemiesInRange.Sort((a, b) =>
            Vector2.Distance(transform.position, a.transform.position)
            .CompareTo(Vector2.Distance(transform.position, b.transform.position)));

        // Activate up to maxActiveEnemies
        int activatedCount = 0;
        HashSet<GameObject> shouldBeActive = new HashSet<GameObject>();

        foreach (GameObject enemy in enemiesInRange)
        {
            if (activatedCount >= maxActiveEnemies) break;

            shouldBeActive.Add(enemy);
            if (!activatedEnemies.Contains(enemy))
            {
                activatedEnemies.Add(enemy);
                Debug.Log($"Activated enemy: {enemy.name} ({activatedCount + 1}/{maxActiveEnemies})");
            }
            activatedCount++;
        }

        // Deactivate enemies that dropped out of the priority window
        var toDeactivate = new List<GameObject>();
        foreach (GameObject enemy in activatedEnemies)
            if (enemy == null || !shouldBeActive.Contains(enemy))
                toDeactivate.Add(enemy);

        foreach (GameObject enemy in toDeactivate)
        {
            activatedEnemies.Remove(enemy);
            if (enemy != null)
            {
                EnemyMovement mv = enemy.GetComponent<EnemyMovement>();
                if (mv != null && mv.IsPathfinding()) mv.DisablePathfinding();
                Debug.Log($"Deactivated enemy: {enemy.name}");
            }
        }

        // Only run pathfinding assignment if the toggle is on
        if (usePathfinding)
            UpdatePathfindingAssignments();
    }

    // -------------------------------------------------------------------------
    // Pathfinding slot management — only called when usePathfinding is true
    // -------------------------------------------------------------------------

    private void UpdatePathfindingAssignments()
    {
        pathfindingEnemies.RemoveWhere(e => e == null);

        // Collect active EnemyMovements
        var activeMovements = new List<EnemyMovement>();
        foreach (GameObject enemy in activatedEnemies)
        {
            if (enemy == null) continue;
            EnemyMovement mv = enemy.GetComponent<EnemyMovement>();
            if (mv != null) activeMovements.Add(mv);
        }

        // Sort closest-first for pathfinding priority
        activeMovements.Sort((a, b) =>
            Vector2.Distance(transform.position, a.transform.position)
            .CompareTo(Vector2.Distance(transform.position, b.transform.position)));

        // Grant pathfinding to eligible leaders within priority radius
        int pfCount = pathfindingEnemies.Count;
        foreach (EnemyMovement enemy in activeMovements)
        {
            if (pfCount >= maxPathfindingEnemies) break;
            if (enemy.IsPathfinding()) continue;

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist <= pathfindingPriorityRadius && enemy.IsLeader())
            {
                TryEnablePathfinding(enemy);
                pfCount = pathfindingEnemies.Count;
            }
        }

        // Revoke pathfinding from enemies that moved outside the priority radius
        var toDisable = new List<EnemyMovement>();
        foreach (EnemyMovement enemy in pathfindingEnemies)
        {
            if (enemy == null) { toDisable.Add(enemy); continue; }
            if (Vector2.Distance(transform.position, enemy.transform.position) > pathfindingPriorityRadius)
                toDisable.Add(enemy);
        }

        foreach (EnemyMovement enemy in toDisable)
        {
            if (enemy != null) enemy.DisablePathfinding();
            else pathfindingEnemies.Remove(enemy);
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public bool IsEnemyActivated(GameObject enemy) => activatedEnemies.Contains(enemy);

    public bool TryEnablePathfinding(EnemyMovement enemy)
    {
        if (enemy == null) return false;
        if (pathfindingEnemies.Contains(enemy)) return true;

        if (pathfindingEnemies.Count >= maxPathfindingEnemies)
        {
            Debug.Log($"Cannot enable pathfinding for {enemy.gameObject.name}: max capacity ({maxPathfindingEnemies})");
            return false;
        }
        if (!activatedEnemies.Contains(enemy.gameObject))
        {
            Debug.Log($"Cannot enable pathfinding for {enemy.gameObject.name}: not activated");
            return false;
        }

        pathfindingEnemies.Add(enemy);
        enemy.EnablePathfinding();
        Debug.Log($"Enabled pathfinding for {enemy.gameObject.name} ({pathfindingEnemies.Count}/{maxPathfindingEnemies})");
        return true;
    }

    public void UnregisterPathfindingEnemy(EnemyMovement enemy)
    {
        if (pathfindingEnemies.Remove(enemy))
            Debug.Log($"Unregistered pathfinding for {enemy.gameObject.name} ({pathfindingEnemies.Count}/{maxPathfindingEnemies})");
    }

    public void ClearAllEnemies()
    {
        activatedEnemies.Clear();
        enemiesInRange.Clear();
        pathfindingEnemies.Clear();
        Debug.Log("ActivateEnemies: Cleared all enemy references");
    }

    public float GetActivationRadius()      => activationRadius;
    public int   GetActiveEnemyCount()      => activatedEnemies.Count;
    public int   GetPathfindingEnemyCount() => pathfindingEnemies.Count;

    // -------------------------------------------------------------------------
    // Gizmos / Debug UI
    // -------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, activationRadius);

        // Only draw pathfinding gizmos when the feature is on
        if (usePathfinding)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, pathfindingPriorityRadius);

            if (Application.isPlaying && pathfindingEnemies != null)
            {
                Gizmos.color = Color.magenta;
                foreach (EnemyMovement enemy in pathfindingEnemies)
                    if (enemy != null) Gizmos.DrawLine(transform.position, enemy.transform.position);
            }
        }
    }

    void OnGUI()
    {
        if (!showDebugGizmos) return;

        GUIStyle style = new GUIStyle { fontSize = 16 };
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 300, 25), $"Active Enemies: {activatedEnemies.Count}/{maxActiveEnemies}", style);

        if (usePathfinding)
            GUI.Label(new Rect(10, 35, 300, 25), $"Pathfinding Enemies: {pathfindingEnemies.Count}/{maxPathfindingEnemies}", style);
        else
            GUI.Label(new Rect(10, 35, 300, 25), "Pathfinding: OFF (sightline mode)", style);
    }
}