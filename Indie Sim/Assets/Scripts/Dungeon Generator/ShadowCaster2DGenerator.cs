using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal; // ✅ UPDATED NAMESPACE

/// <summary>
/// Generates shadow casters for composite colliders at runtime.
/// Works with dynamically generated tilemaps.
/// </summary>
public class ShadowCaster2DGenerator : MonoBehaviour
{
    /// <summary>
    /// Generates shadow casters for a Composite Collider 2D at runtime.
    /// Call this after your dungeon generator finishes creating the tilemap.
    /// </summary>
    public static void GenerateTilemapShadowCasters(CompositeCollider2D collider, bool selfShadows = false)
    {
        if (collider == null)
        {
            Debug.LogWarning("[ShadowCaster2DGenerator] No CompositeCollider2D provided!");
            return;
        }

        // First, destroy existing shadow casters (if regenerating)
        ShadowCaster2D[] existingShadowCasters = collider.GetComponentsInChildren<ShadowCaster2D>();

        foreach (ShadowCaster2D existingCaster in existingShadowCasters)
        {
            if (existingCaster.transform.parent == collider.transform)
            {
                Destroy(existingCaster.gameObject);
            }
        }

        // Create new shadow casters based on composite collider paths
        int pathCount = collider.pathCount;
        List<Vector2> pointsInPath = new List<Vector2>();
        List<Vector3> pointsInPath3D = new List<Vector3>();

        for (int i = 0; i < pathCount; i++)
        {
            collider.GetPath(i, pointsInPath);

            GameObject newShadowCaster = new GameObject($"ShadowCaster2D_Path{i}");
            newShadowCaster.isStatic = true;
            newShadowCaster.transform.SetParent(collider.transform, false);

            // Convert Vector2 to Vector3
            pointsInPath3D.Clear();
            foreach (Vector2 point in pointsInPath)
            {
                pointsInPath3D.Add(point);
            }

            // Add and configure Shadow Caster 2D
            ShadowCaster2D component = newShadowCaster.AddComponent<ShadowCaster2D>();
            component.SetPath(pointsInPath3D.ToArray());
            component.SetPathHash(Random.Range(int.MinValue, int.MaxValue));
            component.selfShadows = selfShadows;

            pointsInPath.Clear();
        }

        Debug.Log($"[ShadowCaster2DGenerator] Generated {pathCount} shadow casters for {collider.gameObject.name}");
    }
}
