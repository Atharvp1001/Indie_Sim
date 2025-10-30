using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class GameSessionData
{
    // Progression
    public int currentStageIndex;
    public int currentLevelIndex;
    public int totalLevelsCompleted;

    // Player Stats
    public float currentHealth;
    public float maxHealth;
    public int coins;

    // Applied Upgrades
    public List<string> appliedUpgradeIds;

    // Stat Modifiers (calculated from upgrades)
    public float damageMultiplier;
    public float attackSpeedMultiplier;
    public float moveSpeedMultiplier;
    public float maxHealthBonus;

    // Stage Transition Flag
    public bool isStageTransition;

    public GameSessionData()
    {
        // Progression defaults
        currentStageIndex = 0;
        currentLevelIndex = 0;
        totalLevelsCompleted = 0;

        // Player stats defaults
        currentHealth = 100f;
        maxHealth = 100f;
        coins = 0;

        // Upgrades
        appliedUpgradeIds = new List<string>();

        // Multipliers defaults
        damageMultiplier = 1f;
        attackSpeedMultiplier = 1f;
        moveSpeedMultiplier = 1f;
        maxHealthBonus = 0f;

        // Flags
        isStageTransition = false;
    }

    /// <summary>
    /// Get the current level number (1-based for display)
    /// </summary>
    public int GetCurrentLevelNumber()
    {
        return currentLevelIndex + 1; // Convert to 1-based
    }

    /// <summary>
    /// Get the current stage number (1-based for display)
    /// </summary>
    public int GetCurrentStageNumber()
    {
        return currentStageIndex + 1; // Convert to 1-based
    }

    /// <summary>
    /// Reset session for new stage/run
    /// </summary>
    public void ResetForNewRun()
    {
        currentHealth = maxHealth;
        currentLevelIndex = 0;
        isStageTransition = false;

        // Keep coins and upgrades
        // Reset other temporary data
        Debug.Log("<color=green>Session reset for new run</color>");
    }
}
