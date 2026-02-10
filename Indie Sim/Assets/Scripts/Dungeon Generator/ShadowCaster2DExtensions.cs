using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal; 

/// <summary>
/// Extends the ShadowCaster2D class to modify private data members.
/// Updated for Unity 6+
/// </summary>
public static class ShadowCaster2DExtensions
{
    /// <summary>
    /// Replaces the path that defines the shape of the shadow caster.
    /// </summary>
    public static void SetPath(this ShadowCaster2D shadowCaster, Vector3[] path)
    {
        FieldInfo shapeField = typeof(ShadowCaster2D).GetField("m_ShapePath",
                                                               BindingFlags.NonPublic |
                                                               BindingFlags.Instance);
        shapeField?.SetValue(shadowCaster, path);
    }

    /// <summary>
    /// Replaces the hash key of the shadow caster to trigger internal rebuild.
    /// </summary>
    public static void SetPathHash(this ShadowCaster2D shadowCaster, int hash)
    {
        FieldInfo hashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash",
                                                              BindingFlags.NonPublic |
                                                              BindingFlags.Instance);
        hashField?.SetValue(shadowCaster, hash);
    }
}
