using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    [SerializeField] private int coinValue = 1;

    private Rigidbody2D rb;
    private bool isBeingCollected = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Auto-setup if Rigidbody2D is missing
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    /// <summary>
    /// Called by PlayerCoinCollector to move coin towards player
    /// </summary>
    public void MoveTowardsPlayer(Vector3 playerPosition, float magnetStrength)
    {
        if (isBeingCollected || rb == null) return;

        Vector2 direction = (playerPosition - transform.position).normalized;

        // Use MovePosition for physics-based movement
        Vector2 newPosition = rb.position + direction * magnetStrength * Time.deltaTime;
        rb.MovePosition(newPosition);
    }

    /// <summary>
    /// Check if coin can be attracted by magnet
    /// </summary>
    public bool CanBeAttracted()
    {
        return !isBeingCollected;
    }

    /// <summary>
    /// Collect this coin (called by PlayerCoinCollector)
    /// </summary>
    public void Collect()
    {
        if (isBeingCollected) return;

        isBeingCollected = true;

        // Add to CoinManager
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(coinValue);
        }

        Debug.Log($"Coin collected! Value: {coinValue}");

        // Destroy coin
        Destroy(gameObject);
    }

    /// <summary>
    /// Get coin value (useful for display or special coins)
    /// </summary>
    public int GetValue()
    {
        return coinValue;
    }
}
