using UnityEngine;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    // Singleton instance
    public static UpgradeManager Instance { get; private set; }

    [Header("Upgrade Increments")]
    public float speedUpgradeAmount = 1f; // How much speed bonus per upgrade
    public int healthUpgradeAmount = 25; // How much health bonus per upgrade

    [Header("Test Buttons")]
    public Button speedUpgradeButton;
    public Button healthUpgradeButton;

    // Current bonus values (start at 0)
    public float speedBonus { get; private set; } = 0f;
    public int healthBonus { get; private set; } = 0;

    // Track upgrade counts
    private int speedUpgradeCount = 0;
    private int healthUpgradeCount = 0;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Add button listeners
        if (speedUpgradeButton != null)
        {
            speedUpgradeButton.onClick.AddListener(UpgradeSpeed);
        }

        if (healthUpgradeButton != null)
        {
            healthUpgradeButton.onClick.AddListener(UpgradeHealth);
        }
    }

    void OnDestroy()
    {
        // Clean up button listeners
        if (speedUpgradeButton != null)
        {
            speedUpgradeButton.onClick.RemoveListener(UpgradeSpeed);
        }

        if (healthUpgradeButton != null)
        {
            healthUpgradeButton.onClick.RemoveListener(UpgradeHealth);
        }
    }

    // Called when speed upgrade button is pressed
    public void UpgradeSpeed()
    {
        speedUpgradeCount++;
        speedBonus = speedUpgradeCount * speedUpgradeAmount;

        Debug.Log($"Speed Upgraded! Level: {speedUpgradeCount}, Speed Bonus: +{speedBonus}");
    }

    // Called when health upgrade button is pressed
    public void UpgradeHealth()
    {
        healthUpgradeCount++;
        int oldBonus = healthBonus;
        healthBonus = healthUpgradeCount * healthUpgradeAmount;
        int healthAdded = healthBonus - oldBonus;

        // Add the extra health to player immediately
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.AddHealth(healthAdded);
        }

        Debug.Log($"Health Upgraded! Level: {healthUpgradeCount}, Health Bonus: +{healthBonus} (added {healthAdded} this upgrade)");
    }

    // Get upgrade counts (for UI display later)
    public int GetSpeedUpgradeCount()
    {
        return speedUpgradeCount;
    }

    public int GetHealthUpgradeCount()
    {
        return healthUpgradeCount;
    }
}
