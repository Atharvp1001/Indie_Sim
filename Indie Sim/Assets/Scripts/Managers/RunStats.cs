using System.Collections.Generic;

/// <summary>
/// Run-scoped data. Lives only in memory for the duration of one run
/// (Main Menu -> death/demo-complete). Reset by GameSession.StartNewRun()
/// constructing a fresh instance — nothing here has its own reset method.
/// Field shapes mirror what each live manager currently tracks; the managers
/// themselves start reading/writing these in Phase 6.
/// </summary>
[System.Serializable]
public class RunStats
{
    // Coins (CoinManager)
    public int CurrentCoins;
    public int CoinsCollectedThisRun;
    public int MaxCoins;

    // Kills (EnemyKillTracker)
    public int KillsThisRun;

    // Relics (RelicManager)
    public bool[] RelicsHeld = new bool[8];
    public int UniqueRelicsCollectedThisRun;

    // Upgrades (UpgradeManager)
    public List<string> AppliedUpgradeIds = new List<string>();
    public int BonusPistolDamage;
    public int BonusPistolAmmo;
    public int BonusShotgunDamage;
    public int BonusShotgunAmmo;
    public int BonusMachineGunDamage;
    public int BonusMachineGunAmmo;
    public float BonusSpeed;
    public float BonusStompRadius;
    public int BonusStompDamage;
    public int BonusCoinCapacity;

    // Weapons (WeaponUnlockManager)
    public List<string> UnlockedWeaponNames = new List<string>();

    // Dungeon progression (UpgradeManager.CurrentDungeonLevel, RoguelikeManager.dungeonsClearedCount)
    public int CurrentDungeonLevel = 1;
    public int DungeonsClearedThisRun;

    // Run timer
    public float RunElapsedSeconds;

    // Player health (PlayerHealth)
    public float CurrentHealth;
    public float MaxHealth;

    // Boss (D1)
    public BossDefinition SelectedBoss;
}
