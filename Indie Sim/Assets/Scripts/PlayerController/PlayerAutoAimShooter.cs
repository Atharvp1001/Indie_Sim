using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAutoAimShooter : MonoBehaviour
{ /*
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask enemyLayers = -1;
    [SerializeField] private PlayerConeShooter playerConeShooter; // Reference to your main shooter script

    [Header("Auto Aim Settings")]
    [SerializeField] private float autoAimRange = 15f; // Max distance to auto aim
    [SerializeField] private float fireRateMultiplier = 1f; // To adjust fire rate for auto aim if needed

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource; // Optional, can use same as player shooter

    [SerializeField] private LayerMask obstacleLayers;

    // Private variables
    private float nextFireTime = 0f;

    private void OnEnable()
    {
        // Make sure playerConeShooter reference is assigned
        if (playerConeShooter == null)
        {
            playerConeShooter = GetComponent<PlayerConeShooter>();
            if (playerConeShooter == null)
            {
                Debug.LogError("PlayerAutoAimShooter requires a PlayerConeShooter reference!");
                enabled = false;
            }
        }
    }

    private void FixedUpdate()
    {
        if (playerConeShooter == null) return;

        WeaponData currentWeapon = playerConeShooter.GetCurrentWeapon();
        if (currentWeapon == null) return;

        // Find nearest target within range
        Collider2D nearestEnemy = FindNearestEnemy();

        if (nearestEnemy != null)
        {
            Vector2 targetDirection = (nearestEnemy.transform.position - firePoint.position).normalized;

            // Check if can fire based on fire rate
            if (Time.time >= nextFireTime)
            {
                FireAtTarget(currentWeapon, targetDirection, nearestEnemy);
                nextFireTime = Time.time + (1f / (currentWeapon.fireRate * fireRateMultiplier));
            }
        }
    }

    private Collider2D FindNearestEnemy()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(firePoint.position, autoAimRange, enemyLayers);
        Collider2D nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider2D col in colliders)
        {
            if (col == null) continue;

            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable == null || damageable.IsDead()) continue;

            float dist = Vector2.Distance(firePoint.position, col.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = col;
            }
        }

        return nearest;
    }

    private void FireAtTarget(WeaponData weapon, Vector2 direction, Collider2D targetCollider)
    {
        // Raycast to check obstacles
        float distToTarget = Vector2.Distance(firePoint.position, targetCollider.transform.position);
        RaycastHit2D hit = Physics2D.Raycast(firePoint.position, direction, distToTarget, obstacleLayers);

        if (hit.collider != null)
        {
            // Obstacle detected, don't shoot
            return;
        }

        IDamageable damageable = targetCollider.GetComponent<IDamageable>();
        if (damageable != null && !damageable.IsDead())
        {
            // Damage the target
            damageable.TakeDamage(weapon.damagePerShot);

            // Call all the particle effects from PlayerConeShooter
            if (playerConeShooter != null)
            {
                //playerConeShooter.PlayShootEffects(); // This plays ALL effects including particles, camera shake, sounds
            }

            // Spawn hit effect at target location
            if (weapon.hitEffect != null)
            {
                Instantiate(weapon.hitEffect, targetCollider.transform.position, Quaternion.identity);
            }

            Debug.Log($"Auto-aim damaged {targetCollider.name} for {weapon.damagePerShot} damage with {weapon.weaponName}");
        }
    }

    /// <summary>
    /// Returns the direction from the player to the current auto-aim target
    /// Returns Vector2.zero if no target is found
    /// </summary>
    public Vector2 GetAutoAimDirection()
    {
        if (firePoint == null) return Vector2.zero;

        // Find the current auto-aim target
        Collider2D nearestEnemy = FindNearestEnemy();

        if (nearestEnemy != null)
        {
            // Calculate direction from fire point to target
            Vector2 direction = (nearestEnemy.transform.position - firePoint.position).normalized;
            return direction;
        }

        return Vector2.zero; // No target found
    }

    */
}
