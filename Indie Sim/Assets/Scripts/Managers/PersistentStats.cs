using System.Collections.Generic;

/// <summary>
/// Persistent data. Survives app launches, reset only by an explicit erase-save.
/// Owned by GameSession.Persistent, loaded/saved as JSON by SaveSystem.
/// </summary>
[System.Serializable]
public class PersistentStats
{
    public int TotalCoinsEverCollected;
    public int TotalEnemiesKilled;
    public List<string> UnlockedAchievementIds = new List<string>();
    public int TotalRuns;
    public int BestRunDungeonsCleared;
    public bool DemoCompleted;

    // Guards the one-time PlayerPrefs -> JSON import (SaveSystem).
    public bool PlayerPrefsMigrated;
}
