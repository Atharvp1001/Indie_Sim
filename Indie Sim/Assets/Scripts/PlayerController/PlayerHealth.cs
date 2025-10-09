using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Damage Cooldown Settings")]
    public float damageCooldown = 1f; // Cooldown time before player can be damaged again
    private float nextDamageTime = 0f; // Tracks when player can be damaged next

    [Header("Visual Feedback Settings")]
    public float flashSpeed = 0.1f; // How fast the sprite flashes
    public float flashDuration = 1f; // How long the flashing lasts

    [Header("Knockback Settings")]
    public float knockbackForce = 5f; // How hard player gets knocked back
    public float knockbackDuration = 0.2f; // How long knockback lasts

    [Header("Death Settings")]
    public Sprite deathSprite; // Sprite to show when player dies
    public float deathDelay = 3f; // How long to wait before showing death UI
    public GameObject deathUIPanel; // UI panel with retry/main menu buttons

    [Header("References")]
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;
    private Rigidbody2D rb;
    private Color originalColor;
    private bool isDead = false;
    private bool isFlashing = false; // Track if currently flashing
    private PlayerController playerController; // Reference to player movement script
    private SimplePlayerRotation playerRotation; // Reference to player rotation script


    void Start()
    {
        currentHealth = maxHealth;

        // Get SpriteRenderer from child object (the player sprite)
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
        playerCollider = GetComponent<Collider2D>();
        playerRotation = GetComponent<SimplePlayerRotation>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            Debug.Log("SpriteRenderer found on child object");
        }
        else
        {
            Debug.LogError("SpriteRenderer not found! Make sure the player sprite child has a SpriteRenderer component.");
        }

        // Make sure death UI is hidden at start
        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(false);
        }

        Debug.Log($"Player initialized with {maxHealth} health");
    }


    /// <summary>
    /// Player takes damage from an enemy
    /// </summary>
    /// <param name="damage">Amount of damage to take</param>
    /// <param name="enemyPosition">Position of the enemy (for knockback direction)</param>
    public void TakeDamage(int damage, Vector3 enemyPosition)
    {
        // Check if player is on cooldown (can't take damage yet)
        if (Time.time < nextDamageTime || isDead)
        {
            Debug.Log("Player is on damage cooldown - cannot take damage yet");
            return;
        }

        // Reduce health
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

        // Set the next time player can take damage (current time + cooldown)
        nextDamageTime = Time.time + damageCooldown;

        // Apply knockback away from enemy
        ApplyKnockback(enemyPosition);

        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Start visual feedback (flashing) but keep collider enabled
            if (!isFlashing)
            {
                StartCoroutine(FlashEffect());
            }
        }
    }

    /// <summary>
    /// Apply knockback force pushing player away from enemy
    /// </summary>
    /// <param name="enemyPosition">Position of the attacking enemy</param>
    private void ApplyKnockback(Vector3 enemyPosition)
    {
        if (rb == null) return;

        // Calculate direction away from enemy
        Vector2 knockbackDirection = (transform.position - enemyPosition).normalized;

        // Apply knockback force (using linearVelocity instead of velocity)
        rb.linearVelocity = Vector2.zero; // Reset current velocity
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        Debug.Log($"Knockback applied in direction: {knockbackDirection}");
    }

    /// <summary>
    /// Visual flashing effect during damage cooldown - COLLIDER STAYS ENABLED
    /// </summary>
    private IEnumerator FlashEffect()
    {
        isFlashing = true;

        // NOTE: We do NOT disable the collider anymore - this fixes both bugs:
        // 1. Enemies can't pass through and overlap with player
        // 2. Player stays inside map bounds

        Debug.Log("Player flashing effect started (damage cooldown active)");

        float elapsedTime = 0f;

        // Flash the sprite during cooldown
        while (elapsedTime < flashDuration)
        {
            // Toggle sprite transparency (flashing effect)
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f); // Low alpha
            }
            yield return new WaitForSeconds(flashSpeed);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor; // Full alpha
            }
            yield return new WaitForSeconds(flashSpeed);

            elapsedTime += flashSpeed * 2;
        }

        // Restore sprite to normal
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        isFlashing = false;
        Debug.Log("Flashing effect ended");
    }

    /// <summary>
    /// Handle player death
    /// </summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("Player died!");

        // Stop any ongoing flash effect
        StopAllCoroutines();

        //stop player movement
        playerController.enabled = false;
        playerRotation.enabled = false;

        // Restore normal color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // Change to death sprite
        if (deathSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = deathSprite;
        }

        // Stop player movement (using linearVelocity instead of velocity)
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic; // Disable physics (using bodyType instead of isKinematic)
        }

        // Disable player collider ONLY on death
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        // Disable player controls (you may need to adjust this based on your movement script)
        // Example: GetComponent<PlayerMovement>().enabled = false;

        // Wait then show death UI
        StartCoroutine(ShowDeathUI());
    }

    /// <summary>
    /// Wait for death delay then show death UI panel
    /// </summary>
    private IEnumerator ShowDeathUI()
    {
        yield return new WaitForSeconds(deathDelay);

        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(true);
            Time.timeScale = 0f; // Pause the game
            Debug.Log("Death UI shown");
        }
    }

    /// <summary>
    /// Retry button - reload current scene
    /// </summary>
    public void Retry()
    {
        Time.timeScale = 1f; // Unpause
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("Restarting level");
    }

    /// <summary>
    /// Main menu button - load main menu scene
    /// </summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Unpause
        SceneManager.LoadScene("MainMenu"); // Change "MainMenu" to your actual main menu scene name
        Debug.Log("Going to main menu");
    }

    // Public getters
    public int GetCurrentHealth() { return currentHealth; }
    public int GetMaxHealth() { return maxHealth; }
    public bool IsDead() { return isDead; }
    public bool IsOnDamageCooldown() { return Time.time < nextDamageTime; } // New getter for cooldown status
}
