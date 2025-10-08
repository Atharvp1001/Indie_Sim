using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Info")]
    public string weaponName;
    public Sprite weaponIcon; // For UI display

    [Header("Shooting Parameters")]
    public float coneAngle = 45f;
    public float coneRange = 8f;
    public float fireRate = 10f;
    public int damagePerShot = 25;

    [Header("Trapezium Shape Settings")]
    [Range(0.1f, 1f)]
    public float baseWidthMultiplier = 0.3f; // How narrow at player (30% of cone angle)
    [Range(0.1f, 1f)]
    public float topWidthMultiplier = 0.7f; // How wide at max range (70% of cone angle)
    [Range(0f, 0.5f)]
    public float baseDistanceRatio = 0.2f; // Where the narrow base starts (20% of range)

    [Header("Shape Profile")]
    public TrapeziumProfile shapeProfile = TrapeziumProfile.Linear;
    [Range(0.5f, 3f)]
    public float expansionCurve = 1f; // Controls how the shape expands (1 = linear)

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound; // For future use

    [Header("Visual Effects")]
    public GameObject muzzleFlashEffect;
    public GameObject hitEffect;

    [Header("Ammo (Optional for future)")]
    public int maxAmmo = -1; // -1 means infinite ammo
    public float reloadTime = 2f;

    // Enum for different expansion profiles
    public enum TrapeziumProfile
    {
        Linear,      // Steady expansion
        EaseIn,      // Slow start, fast expansion
        EaseOut,     // Fast start, slow expansion  
        Curved       // Custom curve based on expansionCurve value
    }

    #region Helper Methods

    /// <summary>
    /// Calculate the allowed angle at a specific distance for this weapon
    /// </summary>
    public float GetAngleAtDistance(float distance)
    {
        if (distance >= coneRange) return GetMaxAngle();
        if (distance <= GetBaseDistance()) return GetBaseAngle();

        // Normalize distance between base and max
        float normalizedDistance = (distance - GetBaseDistance()) / (coneRange - GetBaseDistance());

        // Apply expansion profile
        float curveValue = ApplyExpansionProfile(normalizedDistance);

        // Interpolate between base and max angles
        return Mathf.Lerp(GetBaseAngle(), GetMaxAngle(), curveValue);
    }

    /// <summary>
    /// Get the narrow angle at the base (near player)
    /// </summary>
    public float GetBaseAngle()
    {
        return coneAngle * baseWidthMultiplier;
    }

    /// <summary>
    /// Get the wide angle at maximum range
    /// </summary>
    public float GetMaxAngle()
    {
        return coneAngle * topWidthMultiplier;
    }

    /// <summary>
    /// Get the distance where the base (narrow part) starts
    /// </summary>
    public float GetBaseDistance()
    {
        return coneRange * baseDistanceRatio;
    }

    /// <summary>
    /// Apply the selected expansion profile to the normalized distance
    /// </summary>
    private float ApplyExpansionProfile(float normalizedDistance)
    {
        switch (shapeProfile)
        {
            case TrapeziumProfile.Linear:
                return normalizedDistance;

            case TrapeziumProfile.EaseIn:
                return normalizedDistance * normalizedDistance;

            case TrapeziumProfile.EaseOut:
                return 1f - (1f - normalizedDistance) * (1f - normalizedDistance);

            case TrapeziumProfile.Curved:
                return Mathf.Pow(normalizedDistance, expansionCurve);

            default:
                return normalizedDistance;
        }
    }

    /// <summary>
    /// Get trapezium points for visualization (returns 4 corner points)
    /// </summary>
    public Vector2[] GetTrapeziumPoints(Vector2 origin, Vector2 direction)
    {
        Vector2[] points = new Vector2[4];

        float baseDistance = GetBaseDistance();
        float baseAngle = GetBaseAngle() * Mathf.Deg2Rad;
        float maxAngle = GetMaxAngle() * Mathf.Deg2Rad;

        Vector2 baseCenter = origin + direction * baseDistance;
        Vector2 topCenter = origin + direction * coneRange;

        // Calculate perpendicular direction for width
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);

        // Base (narrow) points
        float baseWidth = baseDistance * Mathf.Tan(baseAngle);
        points[0] = baseCenter - perpendicular * baseWidth; // Base left
        points[1] = baseCenter + perpendicular * baseWidth; // Base right

        // Top (wide) points  
        float topWidth = coneRange * Mathf.Tan(maxAngle);
        points[2] = topCenter + perpendicular * topWidth; // Top right
        points[3] = topCenter - perpendicular * topWidth; // Top left

        return points;
    }

    #endregion

   

  
}
