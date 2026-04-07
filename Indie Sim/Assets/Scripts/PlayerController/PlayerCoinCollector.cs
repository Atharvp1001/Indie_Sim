using UnityEngine;

public class PlayerCoinCollector : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private float magnetRange = 3f;
    [SerializeField] private float magnetStrength = 10f;
    [SerializeField] private float collectDistance = 0.3f; // ✅ How close = collected
    [SerializeField] private LayerMask coinLayer;

    [Header("Performance")]
    [SerializeField] private float checkInterval = 0.1f;

    private float nextCheckTime = 0f;
    private Collider2D[] nearbyCoins = new Collider2D[50];
    private ContactFilter2D contactFilter;

    void Start()
    {
        contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(coinLayer);
        contactFilter.useLayerMask = true;
        contactFilter.useTriggers = true;
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
        int coinCount = Physics2D.OverlapCircle(
            transform.position,
            magnetRange,
            contactFilter,
            nearbyCoins
        );

        for (int i = 0; i < coinCount; i++)
        {
            if (nearbyCoins[i] == null) continue;

            Coin coin = nearbyCoins[i].GetComponent<Coin>();
            if (coin == null || !coin.CanBeAttracted()) continue;

            float distance = Vector2.Distance(transform.position, nearbyCoins[i].transform.position);

            // ✅ Close enough — collect it
            if (distance <= collectDistance)
            {
                coin.Collect();
            }
            else
            {
                // ✅ Still approaching — update target
                coin.StartAttraction(transform.position, magnetStrength);
            }
        }
    }

    #region Debug Gizmos
    private void OnDrawGizmosSelected()
    {
        // Magnet range
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, magnetRange);

        // Collect range
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, collectDistance);
    }
    #endregion
}