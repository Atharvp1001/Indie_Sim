using System.IO;
using UnityEngine;

/// <summary>
/// JSON persistence for PersistentStats, at Application.persistentDataPath.
/// Placed under [Persistent] in Boot.unity per the ownership map, but Load()/
/// Save() are static so GameSession can call them from its own Awake() without
/// depending on this component's Awake() having run first — Unity does not
/// guarantee ordering between sibling components' Awake calls.
/// </summary>
public class SaveSystem : MonoBehaviour
{
    private const string SaveFileName = "persistent_stats.json";
    private const string MigrationCoinsKey = "TotalCoinsEverCollected";
    private const string MigrationKillsKey = "TotalEnemiesKilled";
    private const string MigrationAchievementsKey = "UnlockedAchievements";

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static PersistentStats Load()
    {
        PersistentStats stats = File.Exists(SavePath)
            ? JsonUtility.FromJson<PersistentStats>(File.ReadAllText(SavePath))
            : new PersistentStats();

        if (!stats.PlayerPrefsMigrated)
        {
            MigrateFromPlayerPrefs(stats);
            stats.PlayerPrefsMigrated = true;
            Save(stats);
        }

        return stats;
    }

    public static void Save(PersistentStats stats)
    {
        File.WriteAllText(SavePath, JsonUtility.ToJson(stats, true));
    }

    // One-time PlayerPrefs -> JSON import. Idempotent via PlayerPrefsMigrated,
    // and every field takes the larger of the two values so re-running this
    // (or a pre-existing partial save) can never lose progress.
    private static void MigrateFromPlayerPrefs(PersistentStats stats)
    {
        if (PlayerPrefs.HasKey(MigrationCoinsKey))
            stats.TotalCoinsEverCollected = Mathf.Max(stats.TotalCoinsEverCollected, PlayerPrefs.GetInt(MigrationCoinsKey, 0));

        if (PlayerPrefs.HasKey(MigrationKillsKey))
            stats.TotalEnemiesKilled = Mathf.Max(stats.TotalEnemiesKilled, PlayerPrefs.GetInt(MigrationKillsKey, 0));

        if (PlayerPrefs.HasKey(MigrationAchievementsKey))
        {
            string idsCsv = PlayerPrefs.GetString(MigrationAchievementsKey, "");
            if (!string.IsNullOrEmpty(idsCsv))
            {
                foreach (string id in idsCsv.Split(','))
                {
                    if (!stats.UnlockedAchievementIds.Contains(id))
                        stats.UnlockedAchievementIds.Add(id);
                }
            }
        }

        Debug.Log("[SaveSystem] Migrated legacy PlayerPrefs data into the persistent save.");
    }
}
