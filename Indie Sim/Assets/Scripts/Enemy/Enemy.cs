using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public int maxHealth = 100;
    public GameObject deathEffect;
    public GameObject damageNumberPrefab; // Optional floating damage numbers

    [Header("Visual Feedback")]
    public float flashDuration = 0.1f;
    public Color damageColor = Color.red;

    // Private variables
    private int currentHealth;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Ensure collider is set up correctly
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false; // Enemies should be solid for Physics2D.OverlapCircleAll
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Show damage number (optional)
        if (damageNumberPrefab != null)
        {
            GameObject damageNumber = Instantiate(damageNumberPrefab, transform.position, Quaternion.identity);
            // You can add a script to the damage number prefab to animate and display the damage
        }

        // Flash red when hit
        if (spriteRenderer != null)
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(FlashDamage());
        }

        // Check if enemy died
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Spawn death effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // Add score, play death sound, drop items, etc. here

        // Destroy the enemy
        Destroy(gameObject);
    }

    System.Collections.IEnumerator FlashDamage()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }
        flashCoroutine = null;
    }

    // Public getters for other systems
    public int GetCurrentHealth() { return currentHealth; }
    public int GetMaxHealth() { return maxHealth; }
    public float GetHealthPercentage() { return (float)currentHealth / maxHealth; }
}
