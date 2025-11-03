using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int baseMaxHealth = 100; // Base health without upgrades
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
    public Sprite aliveSprite; // Sprite to show when player is alive

    [Header("References")]
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;
    private Rigidbody2D rb;
    private Color originalColor;
    private bool isDead = false;
    private bool isFlashing = false; // Track if currently flashing
    private PlayerController playerController; // Reference to player movement script
    private SimplePlayerRotation playerRotation; // Reference to player rotation script
    private PlayerAutoAimShooter playerAutoAimShooter; // Reference to auto-aim shooter script
    private PlayerConeShooter playerConeShooter; // Reference to cone shooter script
    private CasualGameModeManager casualGameModeManager;

    void Start()
    {
        // Calculate initial max health with any upgrades
        UpdateMaxHealth();
        currentHealth = maxHealth;

        // Get SpriteRenderer from child object (the player sprite)
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
        playerCollider = GetComponent<Collider2D>();
        playerRotation = GetComponent<SimplePlayerRotation>();
        playerAutoAimShooter = GetComponent<PlayerAutoAimShooter>();
        playerConeShooter = GetComponent<PlayerConeShooter>();
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

        casualGameModeManager = FindObjectOfType<CasualGameModeManager>();
        if (casualGameModeManager == null)
        {
            Debug.LogWarning("CasualGameModeManager not found in scene");
        }
    }

    void Update()
    {
        // Keep max health updated with upgrades
        UpdateMaxHealth();
        Debug.Log("Current Health = " + currentHealth);
    }

    void UpdateMaxHealth()
    {
        // Base health + bonus from UpgradeManager
        if (UpgradeManager.Instance != null)
        {
            maxHealth = baseMaxHealth + UpgradeManager.Instance.healthBonus;
        }
        else
        {
            maxHealth = baseMaxHealth;
        }
    }

    // Called by UpgradeManager to add health immediately
    public void AddHealth(int amount)
    {
        currentHealth += amount;

        // Make sure we don't exceed max health
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        Debug.Log($"Health added: +{amount}. Current Health: {currentHealth}/{maxHealth}");
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

        // Clamp health between 0 and maxHealth (which includes upgrades)
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

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("Player died!");

        // Stop any ongoing flash effect
        StopAllCoroutines();

        // Stop player movement
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        if (playerRotation != null)
        {
            playerRotation.enabled = false;
        }

        // Stop shooting 
        if (playerAutoAimShooter != null)
        {
            playerAutoAimShooter.enabled = false;
        }

        if (playerConeShooter != null)
        {
            playerConeShooter.enabled = false;
        }

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
            rb.bodyType = RigidbodyType2D.Kinematic; // Disable physics
        }

        // Disable player collider ONLY on death
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

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
    /// Retry button - reload current level with specific requirements
    /// </summary>
    public void Retry()
    {
        // 1. Close the death UI panel
        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(false);
        }

        // 2. Reposition player to (0, 0, 0)
        transform.position = Vector3.zero;
        Debug.Log("Player repositioned to (0, 0, 0)");

        // 3. Reset player state (health, visuals, components)
        ResetPlayerStateForRetry();

        // 4. Unpause the game temporarily (tutorial will pause it again)
        Time.timeScale = 1f;

        // 5. Regenerate the current dungeon level
        if (casualGameModeManager != null)
        {
            Debug.Log("Regenerating current dungeon level");
            casualGameModeManager.GenerateCurrentDungeon();
        }

        // 6. Restart the tutorial
        TutorialManager tutorialManager = FindObjectOfType<TutorialManager>();
        if (tutorialManager != null)
        {
            Debug.Log("Restarting tutorial");
            tutorialManager.StartTutorial();
        }
        else
        {
            Debug.LogWarning("TutorialManager not found in scene!");
        }

        // 7. Make sure auto aim shooter stays DISABLED (tutorial will handle enabling it if needed)
        if (playerAutoAimShooter != null)
        {
            playerAutoAimShooter.enabled = false;
            Debug.Log("Auto-aim shooter kept disabled for tutorial");
        }
    }

    /// <summary>
    /// Reset player state for retry - does NOT enable auto-aim shooter
    /// </summary>
    private void ResetPlayerStateForRetry()
    {
        // Reset health
        currentHealth = maxHealth;
        isDead = false;

        // Reset visuals
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            // If you have an alive sprite, restore it here
            spriteRenderer.sprite = aliveSprite;
        }

        // Enable collider
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        // Enable player controller (movement)
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // Enable rotation
        if (playerRotation != null)
        {
            playerRotation.enabled = true;
        }

        // Enable cone shooter
        if (playerConeShooter != null)
        {
            playerConeShooter.enabled = true;
        }

        // Reset rigidbody to dynamic
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }

        Debug.Log("Player state reset for retry (auto-aim NOT enabled)");
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
