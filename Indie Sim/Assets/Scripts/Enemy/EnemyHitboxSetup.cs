using UnityEngine;

/// <summary>
/// Automatically creates a larger hitbox collider for shooting.
/// The main collider stays small for physics/bunching.
/// </summary>
public class EnemyHitboxSetup : MonoBehaviour
{
    [Header("Hitbox Settings")]
    [SerializeField] private float hitboxRadius = 0.7f;
    [SerializeField] private Vector2 hitboxOffset = Vector2.zero;
    [SerializeField] private string hitboxLayer = "EnemyHitbox";

    [Header("Debug")]
    [SerializeField] private bool showHitboxGizmo = true;
    [SerializeField] private Color hitboxGizmoColor = new Color(1f, 0f, 0f, 0.3f);

    private GameObject hitboxObject;
    private CircleCollider2D hitboxCollider;

    private void Awake()
    {
        CreateHitbox();
    }

    /// <summary>
    /// Creates a child GameObject with a larger trigger collider for shooting detection.
    /// </summary>
    private void CreateHitbox()
    {
        // Check if hitbox already exists (in case script runs multiple times)
        Transform existingHitbox = transform.Find("ShootingHitbox");
        if (existingHitbox != null)
        {
            hitboxObject = existingHitbox.gameObject;
            hitboxCollider = hitboxObject.GetComponent<CircleCollider2D>();
            return;
        }

        // Create new child object for hitbox
        hitboxObject = new GameObject("ShootingHitbox");
        hitboxObject.transform.SetParent(transform);
        hitboxObject.transform.localPosition = hitboxOffset;
        hitboxObject.transform.localRotation = Quaternion.identity;

        // Set to hitbox layer
        int layer = LayerMask.NameToLayer(hitboxLayer);
        if (layer == -1)
        {
            Debug.LogError($"[EnemyHitboxSetup] Layer '{hitboxLayer}' not found! Create it in Project Settings → Tags and Layers");
            layer = gameObject.layer; // Fallback to parent layer
        }
        hitboxObject.layer = layer;

        // Add circle collider
        hitboxCollider = hitboxObject.AddComponent<CircleCollider2D>();
        hitboxCollider.radius = hitboxRadius;
        hitboxCollider.isTrigger = true; // MUST be trigger!

        Debug.Log($"[EnemyHitboxSetup] Created shooting hitbox for {gameObject.name} with radius {hitboxRadius}");
    }

    /// <summary>
    /// Visualize the hitbox in the Scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showHitboxGizmo) return;

        Gizmos.color = hitboxGizmoColor;
        Vector3 center = transform.position + (Vector3)hitboxOffset;

        // Draw circle
        DrawCircleGizmo(center, hitboxRadius);
    }

    private void DrawCircleGizmo(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;

        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Context menu to recreate hitbox if you change settings
    /// </summary>
    [ContextMenu("Recreate Hitbox")]
    private void RecreateHitbox()
    {
        // Destroy old hitbox
        Transform existingHitbox = transform.Find("ShootingHitbox");
        if (existingHitbox != null)
        {
            DestroyImmediate(existingHitbox.gameObject);
        }

        // Create new one
        CreateHitbox();
        Debug.Log($"[EnemyHitboxSetup] Hitbox recreated with radius {hitboxRadius}");
    }
#endif
}
