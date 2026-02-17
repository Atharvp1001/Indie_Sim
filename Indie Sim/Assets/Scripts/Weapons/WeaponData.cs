using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Info")]
    public string weaponName;
    public Sprite weaponIcon; // For UI display
    public WeaponType weaponType = WeaponType.Standard;

    [Header("Shooting Parameters")]
    public float coneAngle = 45f;
    public float coneRange = 8f;
    public float fireRate = 10f;
    public int baseDamagePerShot = 10;

    [Header("Trapezium Shape Settings")]
    [Range(0.1f, 1f)]
    public float baseWidthMultiplier = 0.3f;
    [Range(0.1f, 1f)]
    public float topWidthMultiplier = 0.7f;
    [Range(0f, 0.5f)]
    public float baseDistanceRatio = 0.2f;

    [Header("Shape Profile")]
    public TrapeziumProfile shapeProfile = TrapeziumProfile.Linear;
    [Range(0.5f, 3f)]
    public float expansionCurve = 1f;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;

    [Header("Visual Effects")]
    public GameObject muzzleFlashEffect;
    public GameObject hitEffect;

    [Header("Ammo Settings")]
    public int magazineCapacity = 30; // How many bullets per magazine
    public float reloadTime = 2f; // How long reload takes in seconds
    public bool hasInfiniteReserve = true; // Infinite reserve ammo for now

    [Header("Piercing Settings (Piercer only)")]
    [Tooltip("How many enemies the bullet can pierce through (0 = standard behavior)")]
    public int maxPierceCount = 3;

    public enum WeaponType
    {
        Standard,  // Pistol, AK - damages closest enemy only
        Shotgun,  // Shotgun - fires in a cone
        Piercer // shots go thru enemies 
    }

    public enum TrapeziumProfile
    {
        Linear,
        EaseIn,
        EaseOut,
        Curved
    }

    #region Helper Methods

    /// <summary>
    /// Get current weapon damage
    /// </summary>
    public int GetDamage()
    {
        return baseDamagePerShot;
    }

    /// <summary>
    /// Update weapon damage value
    /// Used by UpgradeManager when applying damage upgrades
    /// </summary>
    public void SetDamage(int newDamage)
    {
        baseDamagePerShot = newDamage;
        Debug.Log($"[WeaponData] {weaponName} damage updated to: {newDamage}");
    }

    // KEEP THIS - it returns the current damage (used by PlayerConeShooter)
    public int damagePerShot
    {
        get { return baseDamagePerShot; }
    }

    public float GetAngleAtDistance(float distance)
    {
        if (distance >= coneRange) return GetMaxAngle();
        if (distance <= GetBaseDistance()) return GetBaseAngle();

        float normalizedDistance = (distance - GetBaseDistance()) / (coneRange - GetBaseDistance());
        float curveValue = ApplyExpansionProfile(normalizedDistance);

        return Mathf.Lerp(GetBaseAngle(), GetMaxAngle(), curveValue);
    }

    public float GetBaseAngle()
    {
        return coneAngle * baseWidthMultiplier;
    }

    public float GetMaxAngle()
    {
        return coneAngle * topWidthMultiplier;
    }

    public float GetBaseDistance()
    {
        return coneRange * baseDistanceRatio;
    }

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

    public Vector2[] GetTrapeziumPoints(Vector2 origin, Vector2 direction)
    {
        Vector2[] points = new Vector2[4];

        float baseDistance = GetBaseDistance();
        float baseAngle = GetBaseAngle() * Mathf.Deg2Rad;
        float maxAngle = GetMaxAngle() * Mathf.Deg2Rad;

        Vector2 baseCenter = origin + direction * baseDistance;
        Vector2 topCenter = origin + direction * coneRange;

        Vector2 perpendicular = new Vector2(-direction.y, direction.x);

        float baseWidth = baseDistance * Mathf.Tan(baseAngle);
        points[0] = baseCenter - perpendicular * baseWidth;
        points[1] = baseCenter + perpendicular * baseWidth;

        float topWidth = coneRange * Mathf.Tan(maxAngle);
        points[2] = topCenter + perpendicular * topWidth;
        points[3] = topCenter - perpendicular * topWidth;

        return points;
    }

    #endregion
}
