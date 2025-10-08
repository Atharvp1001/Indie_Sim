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

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound; // For future use

    [Header("Visual Effects")]
    public GameObject muzzleFlashEffect;
    public GameObject hitEffect;

    [Header("Ammo (Optional for future)")]
    public int maxAmmo = -1; // -1 means infinite ammo
    public float reloadTime = 2f;
}
