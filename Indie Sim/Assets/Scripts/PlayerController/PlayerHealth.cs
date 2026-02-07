using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Coin-Based Health Settings")]
    [SerializeField] private int coinsLostPerHit = 100; // How many coins lost when taking damage
    [SerializeField] private bool useCoinsAsHealth = true; // Toggle coin-based health system

    [Header("Coin Drop Visual Feedback")]
    [SerializeField] private GameObject coinPrefab; // Coin prefab to spawn when taking damage
    [SerializeField] private int coinsToSpawnOnDamage = 10; // Number of coin objects to spawn
    [SerializeField] private float coinSpawnRadius = 2f; // How far coins spawn from player
    [SerializeField] private float coinSpawnForce = 5f; // Force applied to spawned coins
    [SerializeField] private float coinLifetime = 2f; // How long coins exist before disappearing

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

    [Header("UI Reference")]
    public HealthHeartBar healthHeartBar;

    [Header("Damage Indicator")]
    [SerializeField] private DamageIndicator damageIndicator;

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

    void Start()
    {
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

        if (damageIndicator == null)
        {
            damageIndicator = GetComponent<DamageIndicator>();
        }

        Debug.Log($"Player initialized with coin-based health system. Coins per hit: {coinsLostPerHit}");
    }

    /// <summary>
    /// Player takes damage - loses coins instead of health
    /// </summary>
    /// <param name="damage">Amount of damage (unused in coin system, we use coinsLostPerHit)</param>
    /// <param name="enemyPosition">Position of the enemy (for knockback direction)</param>
    public void TakeDamage(int damage, Vector3 enemyPosition)
    {
        // Check if player is on cooldown (can't take damage yet)
        if (Time.time < nextDamageTime || isDead)
        {
            Debug.Log("Player is on damage cooldown - cannot take damage yet");
            return;
        }

        if (useCoinsAsHealth)
        {
            // Check if player has enough coins
            if (CoinManager.Instance == null)
            {
                Debug.LogError("CoinManager not found! Cannot process coin-based damage.");
                return;
            }

            int currentCoins = CoinManager.Instance.GetCurrentCoins();

            if (currentCoins <= 0)
            {
                // Player has no coins - instant death
                Die();
                return;
            }

            // Calculate coins to lose (don't go below 0)
            int coinsToLose = Mathf.Min(coinsLostPerHit, currentCoins);

            // Subtract coins from CoinManager
            CoinManager.Instance.SpendCoins(coinsToLose);

            Debug.Log($"Player hit! Lost {coinsToLose} coins. Remaining: {CoinManager.Instance.GetCurrentCoins()}");

            // Spawn visual coin feedback
            SpawnCoinDropVisual(enemyPosition);

            // Trigger damage flash
            if (damageIndicator != null)
            {
                damageIndicator.TriggerDamageFlash();
            }

            // Set the next time player can take damage
            nextDamageTime = Time.time + damageCooldown;

            // Apply knockback away from enemy
            ApplyKnockback(enemyPosition);

            // Check if player is out of coins (death)
            if (CoinManager.Instance.GetCurrentCoins() <= 0)
            {
                Die();
            }
            else
            {
                // Start visual feedback (flashing)
                if (!isFlashing)
                {
                    StartCoroutine(FlashEffect());
                }
            }
        }
        else
        {
            // Legacy health system (if you want to keep it as fallback)
            Debug.LogWarning("Coin-based health is disabled. Enable it in Inspector.");
        }
    }

    /// <summary>
    /// Spawns coins that fly away from player to indicate coin loss
    /// </summary>
    private void SpawnCoinDropVisual(Vector3 damageSourcePosition)
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning("Coin prefab not assigned! Cannot spawn visual feedback.");
            return;
        }

        for (int i = 0; i < coinsToSpawnOnDamage; i++)
        {
            // Random angle for coin spawn
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            // Calculate spawn position in a circle around player
            Vector2 spawnOffset = new Vector2(
                Mathf.Cos(angle) * coinSpawnRadius,
                Mathf.Sin(angle) * coinSpawnRadius
            );

            Vector3 spawnPosition = transform.position + (Vector3)spawnOffset;

            // Instantiate coin
            GameObject coin = Instantiate(coinPrefab, spawnPosition, Quaternion.identity);

            // Make coin non-collectible (disable its collector script if it has one)
            Coin coinScript = coin.GetComponent<Coin>();
            if (coinScript != null)
            {
                coinScript.enabled = false; // Disable collection
            }

            // Apply force away from damage source
            Rigidbody2D coinRb = coin.GetComponent<Rigidbody2D>();
            if (coinRb != null)
            {
                Vector2 forceDirection = ((Vector2)spawnPosition - (Vector2)damageSourcePosition).normalized;
                coinRb.AddForce(forceDirection * coinSpawnForce, ForceMode2D.Impulse);
            }

            // Destroy coin after lifetime
            Destroy(coin, coinLifetime);
        }

        Debug.Log($"Spawned {coinsToSpawnOnDamage} visual coins");
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

        // Apply knockback force
        rb.linearVelocity = Vector2.zero; // Reset current velocity
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        Debug.Log($"Knockback applied in direction: {knockbackDirection}");
    }

    /// <summary>
    /// Visual flashing effect during damage cooldown
    /// </summary>
    private IEnumerator FlashEffect()
    {
        isFlashing = true;

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
        Debug.Log("Player died! (Out of coins)");

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

        // Stop player movement
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
    /// Retry button - reload current level
    /// </summary>
    public void Retry()
    {
        // Close the death UI panel
        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(false);
        }

        // Reposition player to (0, 0, 0)
        transform.position = Vector3.zero;
        Debug.Log("Player repositioned to (0, 0, 0)");

        // Reset player state
        ResetPlayerStateForRetry();

        // Unpause the game
        Time.timeScale = 1f;

        // Restart the tutorial
        TutorialManager tutorialManager = FindObjectOfType<TutorialManager>();
        if (tutorialManager != null)
        {
            Debug.Log("Restarting tutorial");
            tutorialManager.StartTutorial();
        }

        // Keep auto aim shooter disabled (tutorial will enable if needed)
        if (playerAutoAimShooter != null)
        {
            playerAutoAimShooter.enabled = false;
            Debug.Log("Auto-aim shooter kept disabled for tutorial");
        }
    }

    /// <summary>
    /// Reset player state for retry
    /// </summary>
    private void ResetPlayerStateForRetry()
    {
        // Reset death state
        isDead = false;

        // Reset visuals
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
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

        Debug.Log("Player state reset for retry");
    }

    /// <summary>
    /// Main menu button - load main menu scene
    /// </summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Unpause
        SceneManager.LoadScene("MainMenu");
        Debug.Log("Going to main menu");
    }

    // Public getters
    public int GetCurrentHealth()
    {
        if (useCoinsAsHealth && CoinManager.Instance != null)
        {
            return CoinManager.Instance.GetCurrentCoins();
        }
        return 0;
    }

    public int GetMaxHealth()
    {
        if (useCoinsAsHealth && CoinManager.Instance != null)
        {
            return CoinManager.Instance.GetTotalCoinsEverCollected();
        }
        return 100;
    }

    public bool IsDead() { return isDead; }
    public bool IsOnDamageCooldown() { return Time.time < nextDamageTime; }

    /// <summary>
    /// Get how many coins player loses per hit
    /// </summary>
    public int GetCoinsLostPerHit() { return coinsLostPerHit; }
}
