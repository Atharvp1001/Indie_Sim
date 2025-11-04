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

    // UPGRADE TRACKING
    public int speedUpgradeLevel;
    public int healthUpgradeLevel;
    public int pistolDamageBonus;
    public int MachineGunDamageBonus;
    public int shotgunDamageBonus;

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

        // Upgrade tracking defaults
        speedUpgradeLevel = 0;
        healthUpgradeLevel = 0;
        pistolDamageBonus = 0;
        MachineGunDamageBonus = 0;
        shotgunDamageBonus = 0;

        // Flags
        isStageTransition = false;
    }

    /// <summary>
    /// Get the current level number (1-based for display)
    /// </summary>
    public int GetCurrentLevelNumber()
    {
        return currentLevelIndex + 1;
    }

    /// <summary>
    /// Get the current stage number (1-based for display)
    /// </summary>
    public int GetCurrentStageNumber()
    {
        return currentStageIndex + 1;
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
        Debug.Log("<color=green>Session reset for new run</color>");
    }
}
