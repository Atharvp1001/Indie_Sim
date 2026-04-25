using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class RoguelikeManager : MonoBehaviour
{
    public static RoguelikeManager Instance;

    [Header("Store Reference")]
    public StoreManager storeManager;

    [SerializeField] private DungeonMapGenerator dungeonGenerator;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Teleporter teleporter;

    [Header("Demo Settings")]
    [SerializeField] private int roomsTillBoss = 3;
    [SerializeField] private string bossSceneName = "BossLevel";
    [SerializeField] private string roguelikeScene2Name = "RoguelikeModeEmpty";

    private int dungeonsClearedCount = 0;
    private int dungeonSizeIncrement = 0;
    private const int BASE_DUNGEON_SIZE = 5;
    private const float SIZE_INCREASE_CHANCE = 0.5f;
    private int currentLevel = 1;

    private WeaponInventory weaponInventory;
    private WeaponAmmoManager AmmoManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == roguelikeScene2Name)
        {
            Debug.Log("[RoguelikeManager] RoguelikeScene_2 detected — waiting for scene init...");
            StartCoroutine(InitRoguelikeScene2());
        }
        else
        {
            // ✅ Re-grab storeManager for any other roguelike scene reload
            StartCoroutine(ReinitialiseReferences());
        }
    }

    // ✅ NEW — re-grab all scene references on reload
    private IEnumerator ReinitialiseReferences()
    {
        yield return null; // wait one frame for scene to finish loading

        storeManager = FindFirstObjectByType<StoreManager>();
        dungeonGenerator = FindFirstObjectByType<DungeonMapGenerator>();
        playerTransform = FindFirstObjectByType<PlayerController>()?.transform;
        weaponInventory = FindFirstObjectByType<WeaponInventory>();
        AmmoManager = FindFirstObjectByType<WeaponAmmoManager>();

        Debug.Log($"[RoguelikeManager] References re-grabbed — storeManager: {storeManager}");
        Debug.Log($"[RoguelikeManager] dungeonGenerator: {dungeonGenerator}");
        Debug.Log($"[RoguelikeManager] playerTransform: {playerTransform}");

        if (AmmoManager != null && weaponInventory != null)
            AmmoManager.InitialiseAmmo(weaponInventory.GetAllWeapons());
    }

    private IEnumerator InitRoguelikeScene2()
    {
        yield return null;

        dungeonGenerator = FindFirstObjectByType<DungeonMapGenerator>();
        teleporter = FindFirstObjectByType<Teleporter>();
        storeManager = FindFirstObjectByType<StoreManager>();
        playerTransform = FindFirstObjectByType<PlayerController>()?.transform;
        weaponInventory = FindFirstObjectByType<WeaponInventory>();
        AmmoManager = FindFirstObjectByType<WeaponAmmoManager>();

        Debug.Log($"[RoguelikeManager] dungeonGenerator: {dungeonGenerator}");
        Debug.Log($"[RoguelikeManager] teleporter: {teleporter}");
        Debug.Log($"[RoguelikeManager] storeManager: {storeManager}");
        Debug.Log($"[RoguelikeManager] playerTransform: {playerTransform}");

        if (dungeonGenerator == null)
        {
            Debug.LogError("[RoguelikeManager] DungeonMapGenerator not found! Is it in RoguelikeScene_2?");
            yield break;
        }

        GenerateNewDungeon();
        UpdateSpawnerDifficulty();
    }

    private void Start()
    {
        weaponInventory = FindFirstObjectByType<WeaponInventory>();
        AmmoManager = FindFirstObjectByType<WeaponAmmoManager>();
        if (weaponInventory == null)
            Debug.LogError("[RoguelikeManager] WeaponInventory not found in scene!");
        if (AmmoManager == null)
            Debug.LogError("[RoguelikeManager] WeaponAmmoManager not found in scene!");

        GenerateNewDungeon();
        UpdateSpawnerDifficulty();
        AmmoManager.InitialiseAmmo(weaponInventory.GetAllWeapons());
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
        Debug.Log($"[RoguelikeManager] dungeonsClearedCount: {dungeonsClearedCount} / {roomsTillBoss}");
        Debug.Log($"[RoguelikeManager] storeManager is: {storeManager}");
        UpgradeManager.Instance.AdvanceDungeonLevel();

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
        GameManager.Instance.LoadBoss();
    }

    private void ClearCurrentDungeon()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies) Destroy(enemy);
        Debug.Log($"[RoguelikeManager] Cleared {enemies.Length} enemies");

        GameObject[] enemySpawners = GameObject.FindGameObjectsWithTag("EnemySpawner");
        foreach (GameObject spawner in enemySpawners) Destroy(spawner);
        Debug.Log($"[RoguelikeManager] Cleared {enemySpawners.Length} spawners");

        GameObject[] coins = GameObject.FindGameObjectsWithTag("Coin");
        foreach (GameObject coin in coins) Destroy(coin);
        Debug.Log($"[RoguelikeManager] Cleared {coins.Length} coins");
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