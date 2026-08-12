using UnityEngine;

/// <summary>
/// The sole persistent object permitted to hold run-scoped mutable state.
/// Skeleton only: StartNewRun()/EndRun() and the data fields on RunStats/PersistentStats
/// are added in Phase 4. Lives under the [Persistent] root in Boot.unity.
/// </summary>
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public RunStats CurrentRun { get; private set; } = new RunStats();
    public PersistentStats Persistent { get; private set; } = new PersistentStats();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
}
