using System.Collections.Generic;

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

    // Stage Transition Flag (NEW)
    public bool isStageTransition;

    public GameSessionData()
    {
        // Default values
        currentStageIndex = 0;
        currentLevelIndex = 0;
        totalLevelsCompleted = 0;
        currentHealth = 100f;
        maxHealth = 100f;
        coins = 0;
        appliedUpgradeIds = new List<string>();
        damageMultiplier = 1f;
        attackSpeedMultiplier = 1f;
        moveSpeedMultiplier = 1f;
        maxHealthBonus = 0f;
        isStageTransition = false;
    }
}
