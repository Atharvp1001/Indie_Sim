using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Enemy Settings")]
    public int maxHealth = 100;
    public GameObject deathEffect;
    public GameObject damageNumberPrefab; // Optional floating damage numbers

    [Header("Visual Feedback")]
    public float flashDuration = 0.1f;
    public Color damageColor = Color.red;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip deathSound;

    // Private variables
    private int currentHealth;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;
    private bool isDead = false;

    // Events
    public System.Action OnDeath;
    public System.Action<int, int> OnHealthChanged; // current, max

    void Start()
    {
        InitializeEnemy();
    }

    private void InitializeEnemy()
    {
        // Initialize health
        currentHealth = maxHealth;

        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        // Store original color for flash effect
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Ensure collider is set up correctly for cone detection
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            // Can be trigger or solid - both work with Physics2D.OverlapCircleAll
            col.isTrigger = false; // Set to false for solid collision
        }

        Debug.Log($"Enemy '{gameObject.name}' initialized with {maxHealth} health");
    }

    #region IDamageable Interface Implementation

    // INTERFACE METHOD: Must match exactly - public void TakeDamage(int damage)
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        // Reduce health
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Show damage number (optional)
        if (damageNumberPrefab != null)
        {
            GameObject damageNumber = Instantiate(damageNumberPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

            // If the damage number has a Text component, set the damage value
            UnityEngine.UI.Text damageText = damageNumber.GetComponentInChildren<UnityEngine.UI.Text>();
            if (damageText != null)
            {
                damageText.text = damage.ToString();
            }

            // If using TextMeshPro instead
            TMPro.TextMeshProUGUI tmpText = damageNumber.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = damage.ToString();
            }
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

        // Play damage sound
        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        // Notify health change
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"Enemy '{gameObject.name}' took {damage} damage. Health: {currentHealth}/{maxHealth}");

        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // INTERFACE METHOD: Must match exactly - public bool IsDead()
    public bool IsDead()
    {
        return isDead;
    }

    // INTERFACE METHOD: Must match exactly - public GameObject GetGameObject()
    public GameObject GetGameObject()
    {
        return gameObject;
    }

    #endregion

    #region Health System

    public void Heal(int healAmount)
    {
        if (isDead) return;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"Enemy '{gameObject.name}' healed {healAmount}. Health: {currentHealth}/{maxHealth}");
    }

    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        // Notify death (important for spawner tracking)
        OnDeath?.Invoke();

        Debug.Log($"Enemy '{gameObject.name}' died!");

        // Add score, drop items, etc. here
        // Example: GameManager.Instance.AddScore(100);
        // Example: DropLoot();

        // Let EnemyDeath script handle everything
        EnemyDeath deathHandler = GetComponent<EnemyDeath>();
        if (deathHandler != null)
        {
            deathHandler.HandleDeath();
        }
        else
        {
            // Fallback if no EnemyDeath script
            Destroy(gameObject);
        }
    }


   
    #endregion

    #region Visual Effects

    IEnumerator FlashDamage()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(flashDuration);

            // Only restore color if enemy is still alive
            if (!isDead)
            {
                spriteRenderer.color = originalColor;
            }
        }
        flashCoroutine = null;
    }

    #endregion

    #region Public API

    // Public getters for other systems
    public int GetCurrentHealth() { return currentHealth; }
    public int GetMaxHealth() { return maxHealth; }
    public float GetHealthPercentage() { return (float)currentHealth / maxHealth; }
    public bool IsAtFullHealth() { return currentHealth >= maxHealth; }

    // Methods for gameplay systems
    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void InstantKill()
    {
        currentHealth = 0;
        Die();
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw health bar above enemy
        Vector3 healthBarPos = transform.position + Vector3.up * 1.5f;
        float healthPercentage = GetHealthPercentage();

        // Background (red)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(healthBarPos - Vector3.right * 0.5f, healthBarPos + Vector3.right * 0.5f);

        // Health (green)
        Gizmos.color = Color.green;
        Vector3 healthEnd = healthBarPos + Vector3.right * (healthPercentage * 1f - 0.5f);
        Gizmos.DrawLine(healthBarPos - Vector3.right * 0.5f, healthEnd);

        // Dead indicator
        if (isDead)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }

    #endregion
}
