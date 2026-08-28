using UnityEngine;

/// <summary>
/// D4 (Phase 6) — BossArena has no player baked into the scene at all
/// (unlike RoguelikeMode, which keeps its scene-baked instance and gets a
/// fresh one "for free" on every scene reload now that DontDestroyOnLoad is
/// gone). This is BossArena's equivalent: instantiate the player prefab at
/// the scene's PlayerSpawnPoint at runtime instead.
///
/// Runs in Awake(), not Start(), deliberately: Unity calls Awake() on a
/// runtime-Instantiate()'d object's own components synchronously before
/// Instantiate() returns, and every Awake() call across a newly-loaded scene
/// completes before any Start() call fires. So by the time anything else's
/// Start() runs (BossSceneManager spawning the boss, WeaponAmmoManager
/// resolving its UI references, etc.), the player already exists and has
/// already run its own Awake().
///
/// No manual rehydration happens here — every component on the player
/// (CoinManager's health via GameSession, WeaponInventory's equipped weapon,
/// WeaponAmmoManager's ammo, WeaponUnlockManager's unlocks) reads its own
/// starting state from GameSession.CurrentRun in its own Awake()/Start(),
/// per the continuous read/write model. This script's only job is to make
/// the player exist, at the right place, early enough.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

    private void Awake()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] No playerPrefab assigned!");
            return;
        }

        Vector3 position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        if (spawnPoint == null)
            Debug.LogWarning("[PlayerSpawner] No spawnPoint assigned — spawning at origin.");

        Instantiate(playerPrefab, position, rotation);
    }
}
