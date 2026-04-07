using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    [SerializeField] private int coinValue = 1;

    private bool isBeingCollected = false;
    private bool isBeingAttracted = false;
    private Vector2 attractTarget;
    private float attractSpeed = 0f;

    void Update()
    {
        if (!isBeingAttracted || isBeingCollected) return;

        // ✅ Just move the Transform directly — no physics needed
        transform.position = Vector2.MoveTowards(
            transform.position,
            attractTarget,
            attractSpeed * Time.deltaTime
        );
    }

    public void StartAttraction(Vector2 targetPosition, float speed)
    {
        isBeingAttracted = true;
        attractTarget = targetPosition;
        attractSpeed = speed;
    }

    public bool CanBeAttracted() => !isBeingCollected;

    public void Collect()
    {
        if (isBeingCollected) return;
        isBeingCollected = true;

        if (CoinManager.Instance != null)
            CoinManager.Instance.AddCoins(coinValue);

        Debug.Log($"Coin collected! Value: {coinValue}");
        Destroy(gameObject);
    }

    public int GetValue() => coinValue;
}