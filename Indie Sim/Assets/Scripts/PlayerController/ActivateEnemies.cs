using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateEnemies : MonoBehaviour
{
    [Header("Activation Settings")]
    [SerializeField] private float activationRadius = 15f;
    [SerializeField] private float checkInterval = 0.3f;
    [SerializeField] private int maxActiveEnemies = 15;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    public static ActivateEnemies Instance;

    private HashSet<GameObject>     activatedEnemies  = new HashSet<GameObject>();
    private List<GameObject>         enemiesInRange    = new List<GameObject>();

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
            if (activatedEnemies.Count > 0)
            {
                Debug.Log("ActivateEnemies: No enemies in range, clearing all references");
                activatedEnemies.Clear();
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
                Debug.Log($"Deactivated enemy: {enemy.name}");
            }
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public bool IsEnemyActivated(GameObject enemy) => activatedEnemies.Contains(enemy);

    public void ClearAllEnemies()
    {
        activatedEnemies.Clear();
        enemiesInRange.Clear();
        Debug.Log("ActivateEnemies: Cleared all enemy references");
    }

    public float GetActivationRadius()      => activationRadius;
    public int   GetActiveEnemyCount()      => activatedEnemies.Count;

    // -------------------------------------------------------------------------
    // Gizmos / Debug UI
    // -------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }

    void OnGUI()
    {
        if (!showDebugGizmos) return;

        GUIStyle style = new GUIStyle { fontSize = 16 };
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 300, 25), $"Active Enemies: {activatedEnemies.Count}/{maxActiveEnemies}", style);
    }
}