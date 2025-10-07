using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Invincibility Settings")]
    public float invincibilityDuration = 1f; // How long player is invincible after taking damage
    private bool isInvincible = false;
    public float flashSpeed = 0.1f; // How fast the sprite flashes

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

    void Start()
    {
        currentHealth = maxHealth;

        // Get SpriteRenderer from child object (the player sprite)
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        playerCollider = GetComponent<Collider2D>();
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
        // Can't take damage if invincible or dead
        if (isInvincible || isDead) return;

        // Reduce health
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

        // Apply knockback away from enemy
        ApplyKnockback(enemyPosition);

        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Start invincibility frames
            StartCoroutine(InvincibilityFrames());
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
    /// Invincibility frames with flashing sprite effect
    /// </summary>
    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        // Disable collider during invincibility
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        Debug.Log("Player is now invincible");

        float elapsedTime = 0f;

        // Flash the sprite during invincibility
        while (elapsedTime < invincibilityDuration)
        {
            // Toggle sprite visibility (flashing effect)
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f); // Low alpha
            yield return new WaitForSeconds(flashSpeed);

            spriteRenderer.color = originalColor; // Full alpha
            yield return new WaitForSeconds(flashSpeed);

            elapsedTime += flashSpeed * 2;
        }

        // Restore sprite to normal
        spriteRenderer.color = originalColor;

        // Re-enable collider
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        isInvincible = false;
        Debug.Log("Invincibility ended");
    }

    /// <summary>
    /// Handle player death
    /// </summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("Player died!");

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

        // Disable player collider
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
    public bool IsInvincible() { return isInvincible; }
}
