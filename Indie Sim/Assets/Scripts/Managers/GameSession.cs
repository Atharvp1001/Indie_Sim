using UnityEngine;

/// <summary>
/// The sole persistent object permitted to hold run-scoped mutable state.
/// Lives under the [Persistent] root in Boot.unity.
/// </summary>
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public RunStats CurrentRun { get; private set; } = new RunStats();
    public PersistentStats Persistent { get; private set; }

    // Guards EndRun() against double-invocation (D2: death and boss-defeat
    // both route into it). Cleared by StartNewRun().
    private bool _runEnding;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Persistent = SaveSystem.Load();
    }

    public void Save() => SaveSystem.Save(Persistent);

    /// <summary>One call site for resetting run-scoped data (core principle).</summary>
    public void StartNewRun()
    {
        Debug.Log("[GameSession] StartNewRun() called.");
        CurrentRun = new RunStats();
        Persistent.TotalRuns++;
        _runEnding = false;

        BridgeLegacyManagerResets();
    }

    // TEMP (Phase 5): the managers below are still DontDestroyOnLoad, so their
    // own Awake()-time resets never re-fire after the first run (AUDIT.md §5.4).
    // Bridges directly to their reset methods until Phase 6 makes them
    // scene-local, at which point reset happens naturally because the objects
    // are new and this method goes away entirely.
    private void BridgeLegacyManagerResets()
    {
        Debug.Log($"[GameSession] BridgeLegacyManagerResets() — CoinManager.Instance:{CoinManager.Instance != null}, EnemyKillTracker.Instance:{EnemyKillTracker.Instance != null}, UpgradeManager.Instance:{UpgradeManager.Instance != null}, WeaponUnlockManager.Instance:{WeaponUnlockManager.Instance != null}");

        CoinManager.Instance?.ResetForNewRun();
        EnemyKillTracker.Instance?.ResetRunKills();
        UpgradeManager.Instance?.ResetRunData();
        WeaponUnlockManager.Instance?.ResetForNewRun();

        // The player is also still DontDestroyOnLoad — the same dead
        // GameObject would otherwise survive the retry (found via testing,
        // same bug class, not in the plan's original four-manager list).
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        Debug.Log($"[GameSession] BridgeLegacyManagerResets() — PlayerHealth found: {playerHealth != null}");
        playerHealth?.ResetForNewRun();
    }

    /// <summary>
    /// Folds run results into PersistentStats and saves. Not called from
    /// anywhere yet — Phase 5 wires death/boss-defeat/quit into this.
    /// </summary>
    public void EndRun(bool completed)
    {
        Debug.Log($"[GameSession] EndRun(completed:{completed}) called. _runEnding was {_runEnding}.");
        if (_runEnding) return;
        _runEnding = true;

        Persistent.BestRunDungeonsCleared = Mathf.Max(Persistent.BestRunDungeonsCleared, CurrentRun.DungeonsClearedThisRun);
        if (completed)
            Persistent.DemoCompleted = true;

        Save();
    }
}
