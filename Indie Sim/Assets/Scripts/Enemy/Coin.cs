using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    public int coinValue = 1; // How much this coin is worth
    public float magnetRange = 2f; // Distance at which coin moves to player
    public float magnetSpeed = 5f; // Speed at which coin moves to player

    private Transform playerTransform;
    private bool isBeingCollected = false;

    void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        // If close enough to player, move towards them
        if (playerTransform != null && !isBeingCollected)
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);

            if (distance < magnetRange)
            {
                // Move towards player
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    playerTransform.position,
                    magnetSpeed * Time.deltaTime
                );
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            CoinManager.Instance.AddCoins(coinValue);
            Destroy(gameObject);
        }
    }

    void CollectCoin()
    {
        if (isBeingCollected) return;

        isBeingCollected = true;

        // Add coin to player's inventory/currency
        // Example: GameManager.Instance.AddCoins(coinValue);
        // Or: other.GetComponent<PlayerInventory>().AddCoins(coinValue);

        Debug.Log($"Coin collected! Value: {coinValue}");

        // Destroy the coin
        Destroy(gameObject);
    }
}
