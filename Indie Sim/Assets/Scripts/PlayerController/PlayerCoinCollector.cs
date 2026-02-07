using UnityEngine;

public class PlayerCoinCollector : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private float magnetRange = 3f;
    [SerializeField] private float magnetStrength = 10f;
    [SerializeField] private LayerMask coinLayer; // ✅ Assign "Coin" layer in Inspector

    [Header("Performance")]
    [SerializeField] private float checkInterval = 0.1f;

    private float nextCheckTime = 0f;
    private Collider2D[] nearbyCoins = new Collider2D[50]; // Reusable array
    private ContactFilter2D contactFilter;

    void Start()
    {
        // Setup contact filter with LayerMask
        contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(coinLayer); // ✅ Only detect coins
        contactFilter.useLayerMask = true;
    }

    void Update()
    {
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            AttractNearbyCoins();
        }
    }

    private void AttractNearbyCoins()
    {
        // Use OverlapCircle with LayerMask filter
        int coinCount = Physics2D.OverlapCircle(
            transform.position,
            magnetRange,
            contactFilter,
            nearbyCoins
        );

        // Apply magnet force to each coin found
        for (int i = 0; i < coinCount; i++)
        {
            if (nearbyCoins[i] == null) continue;

            Coin coin = nearbyCoins[i].GetComponent<Coin>();
            if (coin != null && coin.CanBeAttracted())
            {
                coin.MoveTowardsPlayer(transform.position, magnetStrength);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Coin coin = collision.gameObject.GetComponent<Coin>();
        if (coin != null)
        {
            coin.Collect();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Coin coin = collision.GetComponent<Coin>();
        if (coin != null)
        {
            coin.Collect();
        }
    }

    #region Debug Gizmos
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, magnetRange);
    }
    #endregion
}
