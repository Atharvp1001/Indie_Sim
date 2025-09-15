using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerConeShooter : MonoBehaviour
{
    [Header("Shooting References")]
    [SerializeField] private FixedJoystick shootingJoystick; // Drag your shooting joystick here
    [SerializeField] private Transform firePoint; // Where the cone originates from

    [Header("Cone Settings")]
    [SerializeField] private float coneAngle = 45f; // Total angle of the cone in degrees
    [SerializeField] private float coneRange = 8f; // How far the cone reaches
    [SerializeField] private LayerMask enemyLayers = -1; // What layers count as enemies
    [SerializeField] private bool showConeInEditor = true; // Visualize cone in scene view

    [Header("Shooting Settings")]
    [SerializeField] private float fireRate = 10f; // Shots per second (high for boomer shooter feel)
    [SerializeField] private int damagePerShot = 25; // Damage per shot
    [SerializeField] private float joystickDeadZone = 0.1f; // Minimum joystick input to shoot

    [Header("Visual Effects")]
    [SerializeField] private GameObject muzzleFlashEffect; // Optional muzzle flash
    [SerializeField] private GameObject hitEffect; // Optional hit effect on enemies
    [SerializeField] private LineRenderer coneVisualizer; // Optional visual cone (for gameplay feedback)

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;

    // Private variables
    private float nextFireTime = 0f;
    private bool wasShooting = false;
    private List<Enemy> enemiesInCone = new List<Enemy>();

    private void FixedUpdate()
    {
        // Get joystick direction
        Vector2 shootDirection = new Vector2(shootingJoystick.Horizontal, shootingJoystick.Vertical);

        // Check if we should shoot
        bool shouldShoot = shootDirection.magnitude > joystickDeadZone;

        if (shouldShoot)
        {
            shootDirection = shootDirection.normalized;

            // Update cone visual if we have one
            if (coneVisualizer != null)
            {
                UpdateConeVisual(shootDirection);
            }

            // Check if it's time to fire
            if (Time.time >= nextFireTime)
            {
                FireCone(shootDirection);
                nextFireTime = Time.time + (1f / fireRate);
            }

            wasShooting = true;
        }
        else
        {
            // Hide cone visual when not shooting
            if (coneVisualizer != null)
            {
                coneVisualizer.enabled = false;
            }

            if (wasShooting)
            {
                OnStopShooting();
            }
            wasShooting = false;
        }
    }

    private void FireCone(Vector2 direction)
    {
        // Clear previous frame's enemies
        enemiesInCone.Clear();

        // Find all enemies in the cone
        DetectEnemiesInCone(direction);

        // Damage all enemies in cone
        foreach (Enemy enemy in enemiesInCone)
        {
            if (enemy != null)
            {
                enemy.TakeDamage(damagePerShot);

                // Spawn hit effect
                if (hitEffect != null)
                {
                    Instantiate(hitEffect, enemy.transform.position, Quaternion.identity);
                }
            }
        }

        // Visual and audio effects
        PlayShootEffects();
    }

    private void DetectEnemiesInCone(Vector2 direction)
    {
        // Get all colliders in range
        Collider2D[] colliders = Physics2D.OverlapCircleAll(firePoint.position, coneRange, enemyLayers);

        foreach (Collider2D collider in colliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy == null) continue;

            // Calculate direction to enemy
            Vector2 directionToEnemy = (collider.transform.position - firePoint.position).normalized;

            // Calculate angle between shoot direction and enemy direction
            float angleToEnemy = Vector2.Angle(direction, directionToEnemy);

            // Check if enemy is within cone angle
            if (angleToEnemy <= coneAngle * 0.5f) // Half angle because Vector2.Angle gives the full angle
            {
                enemiesInCone.Add(enemy);
            }
        }
    }

    private void PlayShootEffects()
    {
        // Muzzle flash
        if (muzzleFlashEffect != null)
        {
            GameObject flash = Instantiate(muzzleFlashEffect, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.1f); // Quick flash
        }

        // Shoot sound
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }

    private void UpdateConeVisual(Vector2 direction)
    {
        if (coneVisualizer == null) return;

        coneVisualizer.enabled = true;

        // Calculate cone edges
        float halfAngle = coneAngle * 0.5f * Mathf.Deg2Rad;

        // Create cone points
        Vector3 centerDirection = new Vector3(direction.x, direction.y, 0) * coneRange;
        Vector3 leftEdge = Quaternion.Euler(0, 0, coneAngle * 0.5f) * centerDirection;
        Vector3 rightEdge = Quaternion.Euler(0, 0, -coneAngle * 0.5f) * centerDirection;

        // Set line renderer points
        coneVisualizer.positionCount = 4;
        coneVisualizer.SetPosition(0, firePoint.position); // Origin
        coneVisualizer.SetPosition(1, firePoint.position + leftEdge); // Left edge
        coneVisualizer.SetPosition(2, firePoint.position + rightEdge); // Right edge
        coneVisualizer.SetPosition(3, firePoint.position); // Back to origin
    }

    private void OnStopShooting()
    {
        // Any cleanup when stopping shooting
    }

    // Public methods for upgrades/power-ups
    public void SetConeAngle(float newAngle)
    {
        coneAngle = Mathf.Clamp(newAngle, 5f, 180f);
    }

    public void SetConeRange(float newRange)
    {
        coneRange = Mathf.Max(1f, newRange);
    }

    public void SetFireRate(float newFireRate)
    {
        fireRate = Mathf.Max(0.1f, newFireRate);
    }

    public void SetDamage(int newDamage)
    {
        damagePerShot = Mathf.Max(1, newDamage);
    }

    // Get current shooting info
    public bool IsShooting() { return wasShooting; }
    public Vector2 GetShootingDirection()
    {
        Vector2 direction = new Vector2(shootingJoystick.Horizontal, shootingJoystick.Vertical);
        return direction.magnitude > joystickDeadZone ? direction.normalized : Vector2.zero;
    }

    // Debug visualization in Scene view
    private void OnDrawGizmosSelected()
    {
        if (!showConeInEditor || firePoint == null) return;

        Vector2 shootDirection = GetShootingDirection();
        if (shootDirection == Vector2.zero)
        {
            shootDirection = Vector2.right; // Default direction for visualization
        }

        // Draw cone
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(firePoint.position, coneRange);

        // Draw cone edges
        float halfAngle = coneAngle * 0.5f;
        Vector3 centerDirection = new Vector3(shootDirection.x, shootDirection.y, 0) * coneRange;
        Vector3 leftEdge = Quaternion.Euler(0, 0, halfAngle) * centerDirection;
        Vector3 rightEdge = Quaternion.Euler(0, 0, -halfAngle) * centerDirection;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(firePoint.position, firePoint.position + leftEdge);
        Gizmos.DrawLine(firePoint.position, firePoint.position + rightEdge);
        Gizmos.DrawLine(firePoint.position + leftEdge, firePoint.position + rightEdge);
    }
}
