using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TilemapShadowCaster2D : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Drag your Composite Collider 2D here (should be on same GameObject)")]
    private CompositeCollider2D m_TilemapCollider;

    [SerializeField]
    [Tooltip("Enable to make shadows cast on the object itself")]
    private bool m_SelfShadows = false;

    [SerializeField]
    [Tooltip("Delay in seconds before generating (useful if tilemap generates slowly)")]
    private float m_GenerationDelay = 0.1f;

    private void Reset()
    {
        m_TilemapCollider = GetComponent<CompositeCollider2D>();
    }

    private void Start()
    {
        Invoke(nameof(GenerateShadows), m_GenerationDelay);
    }

    private void GenerateShadows()
    {
        if (m_TilemapCollider == null)
        {
            Debug.LogError($"[TilemapShadowCaster2D] No Composite Collider 2D assigned on {gameObject.name}!");
            return;
        }

        // ✅ Force the composite collider to rebuild its geometry first
        m_TilemapCollider.GenerateGeometry();

        ShadowCaster2DGenerator.GenerateTilemapShadowCasters(m_TilemapCollider, m_SelfShadows);
    }

    /// <summary>
    /// Call this from your dungeon generator after tilemap is complete.
    /// </summary>
    public void RegenerateShadows()
    {
        //ClearShadows();
        Invoke(nameof(GenerateShadows), m_GenerationDelay); // ✅ Small delay so tilemap finishes
    }

    /// <summary>
    /// Destroys all ShadowCaster2D child GameObjects entirely (not just the component).
    /// </summary>
    public void ClearShadows()
    {
        // ✅ Collect all children named like "ShadowCaster2D_Path*"
        // We go backwards to safely destroy while iterating
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

            // ✅ Destroy the entire GameObject, not just the component
            if (child.name.StartsWith("ShadowCaster2D"))
            {
                DestroyImmediate(child);
            }
        }

        Debug.Log("[TilemapShadowCaster2D] Shadow caster GameObjects cleared.");
    }
}