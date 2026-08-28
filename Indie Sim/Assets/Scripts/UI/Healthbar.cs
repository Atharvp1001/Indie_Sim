using System.Collections.Generic;
using UnityEngine;

public class HealthHeartBar : MonoBehaviour
{
    [Header("Heart Prefab")]
    public GameObject heartPrefab; // Prefab with just an Image component (full heart sprite)

    [Header("Player Reference")]
    public PlayerHealth playerHealth; // Reference to player health script

    [Header("Heart Settings")]
    public int healthPerHeart = 25; // Each heart = 25 HP

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // List to track all spawned hearts
    private List<GameObject> hearts = new List<GameObject>();

    private int lastHealth = 0;

    private void Start()
    {
        // Auto-find PlayerHealth if not assigned
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogError("[HealthHeartBar] PlayerHealth not found in scene!");
                return;
            }
        }

        // Create initial hearts based on starting health
        lastHealth = playerHealth.GetCurrentHealth();
        UpdateHearts();
    }

    private void Update()
    {
        int currentHealth = playerHealth.GetCurrentHealth();

        // Only update if health changed
        if (currentHealth != lastHealth)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[HealthHeartBar] Health changed from {lastHealth} to {currentHealth}");
            }

            UpdateHearts();
            lastHealth = currentHealth;
        }
    }

    /// <summary>
    /// Update hearts based on current health
    /// Creates or destroys hearts as needed
    /// </summary>
    private void UpdateHearts()
    {
        int currentHealth = playerHealth.GetCurrentHealth();
        int heartsNeeded = Mathf.CeilToInt((float)currentHealth / healthPerHeart);

        // Make sure we have the correct number of hearts
        while (hearts.Count < heartsNeeded)
        {
            // Need more hearts - create one
            CreateHeart();
        }

        while (hearts.Count > heartsNeeded)
        {
            // Too many hearts - destroy one
            DestroyHeart();
        }

        if (showDebugLogs)
        {
            Debug.Log($"[HealthHeartBar] Current hearts: {hearts.Count} (Health: {currentHealth})");
        }
    }

    /// <summary>
    /// Create a new heart and add to the list
    /// </summary>
    private void CreateHeart()
    {
        GameObject newHeart = Instantiate(heartPrefab, transform);
        hearts.Add(newHeart);

        if (showDebugLogs)
        {
            Debug.Log($"[HealthHeartBar] Heart created. Total hearts: {hearts.Count}");
        }
    }

    /// <summary>
    /// Destroy the last heart in the list
    /// </summary>
    private void DestroyHeart()
    {
        if (hearts.Count == 0) return;

        // Get last heart
        int lastIndex = hearts.Count - 1;
        GameObject heartToDestroy = hearts[lastIndex];

        // Remove from list
        hearts.RemoveAt(lastIndex);

        // Destroy the GameObject
        Destroy(heartToDestroy);

        if (showDebugLogs)
        {
            Debug.Log($"[HealthHeartBar] Heart destroyed. Remaining hearts: {hearts.Count}");
        }
    }

    /// <summary>
    /// Force refresh all hearts (call from UpgradeManager)
    /// </summary>
    public void RefreshHearts()
    {
        UpdateHearts();

        if (showDebugLogs)
        {
            Debug.Log("[HealthHeartBar] Hearts manually refreshed");
        }
    }

    /// <summary>
    /// Clear all hearts (cleanup)
    /// </summary>
    private void ClearAllHearts()
    {
        foreach (GameObject heart in hearts)
        {
            if (heart != null)
            {
                Destroy(heart);
            }
        }

        hearts.Clear();
    }

    private void OnDestroy()
    {
        ClearAllHearts();
    }
}
