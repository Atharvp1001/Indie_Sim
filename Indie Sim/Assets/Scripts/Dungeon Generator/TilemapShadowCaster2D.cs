using UnityEngine;
using UnityEngine.Rendering.Universal; // ✅ UPDATED NAMESPACE

/// <summary>
/// Attach this to your wall tilemap GameObject.
/// It automatically generates shadow casters after the tilemap is created.
/// </summary>
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
        // Auto-assign if on same GameObject
        m_TilemapCollider = GetComponent<CompositeCollider2D>();
    }

    private void Start()
    {
        // Generate shadows after a short delay
        Invoke(nameof(GenerateShadows), m_GenerationDelay);
    }

    private void GenerateShadows()
    {
        if (m_TilemapCollider == null)
        {
            Debug.LogError($"[TilemapShadowCaster2D] No Composite Collider 2D assigned on {gameObject.name}!");
            return;
        }

        ShadowCaster2DGenerator.GenerateTilemapShadowCasters(m_TilemapCollider, m_SelfShadows);
    }

    /// <summary>
    /// Call this manually from your dungeon generator after tilemap is complete.
    /// </summary>
    public void RegenerateShadows()
    {
        GenerateShadows();
    }
}
