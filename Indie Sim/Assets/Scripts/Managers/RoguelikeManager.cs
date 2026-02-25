using UnityEngine;
using UnityEngine.SceneManagement;

public class RoguelikeManager : MonoBehaviour
{
    [Header("Store Reference")]
    public StoreManager storeManager;

    [SerializeField] private DungeonMapGenerator dungeonGenerator;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Teleporter teleporter;

    [Header("Demo Settings")]
    [SerializeField] private int roomsTillBoss = 3;
    [SerializeField] private string bossSceneName = "BossLevel";

    [Header("Persistent Managers")]
    [SerializeField] private GameObject persistentCanvas;
    [SerializeField] private GameObject upgradeManager;
    [SerializeField] private GameObject enemyKillTracker;
    [SerializeField] private GameObject coinManager;
    [SerializeField] private GameObject musicManager;
    [SerializeField] private GameObject crosshairController;

    private int dungeonsClearedCount = 0;
    private int dungeonSizeIncrement = 0;
    private const int BASE_DUNGEON_SIZE = 5;
    private const float SIZE_INCREASE_CHANCE = 0.5f;
    private int currentLevel = 1;

    private void Start()
    {
        GenerateNewDungeon();
        UpdateSpawnerDifficulty();
    }

    private int GetCurrentDungeonSize()
    {
        return BASE_DUNGEON_SIZE + dungeonSizeIncrement;
    }

    public void GenerateNewDungeon()
    {
        int dungeonSize = GetCurrentDungeonSize();
        Debug.Log($"[RoguelikeManager] Generating dungeon #{dungeonsClearedCount + 1} (Level {currentLevel})");
        dungeonGenerator.GenerateNewMap(dungeonSize);
        Debug.Log($"[RoguelikeManager] Dungeon generation complete!");
    }

    public void CompleteDungeon()
    {
        dungeonsClearedCount++;
        Debug.Log($"[RoguelikeManager] Dungeon #{dungeonsClearedCount} completed!");

        ClearCurrentDungeon();

        if (dungeonsClearedCount >= roomsTillBoss)
        {
            Debug.Log($"<color=red>[RoguelikeManager] {roomsTillBoss} dungeons cleared — heading to Boss!</color>");
            LoadBossLevel();
            return;
        }

        if (storeManager != null)
            storeManager.OpenStore();
        else
            Debug.LogError("[RoguelikeManager] StoreManager not assigned!");
    }

    private void LoadBossLevel()
    {
        if (playerTransform != null)
        {
            DontDestroyOnLoad(playerTransform.gameObject);

            // ✅ SceneTransitionHandler on the Player handles post-load setup
            SceneTransitionHandler handler = playerTransform.GetComponent<SceneTransitionHandler>();
            if (handler != null)
                handler.HandleBossSceneLoad();
            else
                Debug.LogWarning("[RoguelikeManager] SceneTransitionHandler not found on Player!");
        }

        if (persistentCanvas != null)
            DontDestroyOnLoad(persistentCanvas);

        PersistIfNotNull(upgradeManager);
        PersistIfNotNull(enemyKillTracker);
        PersistIfNotNull(coinManager);
        PersistIfNotNull(musicManager);
        PersistIfNotNull(crosshairController);

        // ✅ RoguelikeManager does NOT persist — it dies here, that's intentional
        SceneManager.LoadScene(bossSceneName);
    }

    private void PersistIfNotNull(GameObject obj)
    {
        if (obj != null)
            DontDestroyOnLoad(obj);
        else
            Debug.LogWarning("[RoguelikeManager] A manager reference is null — check Inspector assignments!");
    }

    // ✅ REMOVED — OnBossSceneLoaded() no longer lives here
    // It now lives in SceneTransitionHandler.cs on the Player GameObject

    private void ClearCurrentDungeon()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies) Destroy(enemy);
        Debug.Log($"[RoguelikeManager] Cleared {enemies.Length} enemies");

        GameObject[] enemySpawners = GameObject.FindGameObjectsWithTag("EnemySpawner");
        foreach (GameObject spawner in enemySpawners) Destroy(spawner);
        Debug.Log($"[RoguelikeManager] Cleared {enemySpawners.Length} spawners");
    }

    public void ContinueDungeon()
    {
        Time.timeScale = 1f;

        currentLevel++;
        Debug.Log($"[RoguelikeManager] Level increased to {currentLevel}!");

        float randomRoll = Random.value;
        if (randomRoll <= SIZE_INCREASE_CHANCE)
        {
            dungeonSizeIncrement++;
            Debug.Log($"[RoguelikeManager] Size increased! Difficulty: {dungeonSizeIncrement}");
        }

        playerTransform.position = Vector3.zero;
        GenerateNewDungeon();
        UpdateSpawnerDifficulty();
    }

    private void UpdateSpawnerDifficulty()
    {
        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>();
        foreach (EnemySpawner spawner in spawners)
            spawner.UpdateDifficultyForLevel(currentLevel);

        Debug.Log($"[RoguelikeManager] {spawners.Length} spawners updated for Level {currentLevel}");
    }

    public void OnLevelStart(int levelNumber)
    {
        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>();
        foreach (EnemySpawner spawner in spawners)
            spawner.UpdateDifficultyForLevel(levelNumber);
    }

    [ContextMenu("DEBUG - Skip Current Dungeon")]
    public void DEBUG_SkipDungeon() => CompleteDungeon();

    [ContextMenu("DEBUG - Print Progression Stats")]
    public void DEBUG_PrintStats()
    {
        Debug.Log($"╔════════════ ROGUELIKE PROGRESSION STATS ════════════╗");
        Debug.Log($"║ Current Level:        {currentLevel}");
        Debug.Log($"║ Dungeons Cleared:     {dungeonsClearedCount} / {roomsTillBoss}");
        Debug.Log($"║ Difficulty Increments:{dungeonSizeIncrement}");
        Debug.Log($"║ Current Dungeon Size: {GetCurrentDungeonSize()} nodes");
        Debug.Log($"╚═════════════════════════════════════════════════════╝");
    }

    [ContextMenu("DEBUG - Reset All Progression")]
    public void DEBUG_ResetProgression()
    {
        dungeonsClearedCount = 0;
        dungeonSizeIncrement = 0;
        currentLevel = 1;
        GenerateNewDungeon();
        UpdateSpawnerDifficulty();
        DEBUG_PrintStats();
    }

    public int GetDungeonsClearedCount() => dungeonsClearedCount;
    public int GetDungeonSizeIncrement() => dungeonSizeIncrement;
    public int GetCurrentDungeonNodeCount() => GetCurrentDungeonSize();
    public int GetCurrentLevel() => currentLevel;
}
