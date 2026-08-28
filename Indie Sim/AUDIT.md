# AUDIT.md — Pre-Refactor State Map

_Generated 2026-08-12. Read-only research — no code, scenes, or settings were modified to produce this document. This is a snapshot of `Assets/Scripts` and `Assets/Scenes` as they currently exist on the `Sarbo` branch._

---

## 1. Singletons

### Method note

Two files that appear in searches for `Instance`/`DontDestroyOnLoad` — **`Managers/PersistentDataManager.cs`** and **`Managers/StageUnlockManager.cs`** — are **entirely wrapped in a `/* ... */` block comment**. They do not compile and do not exist at runtime. Same for **`Managers/CasualModeManager.cs`** (class `CasualGameModeManager`) and **`UI/StageButton.cs`**. Any reference to these below is describing dead text, not live behavior — flagged again in Sections 3 and 5, and probably the single most load-bearing finding in this document for planning a persistence refactor.

Scene presence was checked by resolving each script's GUID and grepping scene YAML directly, **and** by resolving prefabs that carry the script (since prefab instances in scene files often only list a `PrefabInstance` + override diffs, not every child component's GUID).

### Inventory

| Script | Awake dedupes duplicates | DontDestroyOnLoad | Referenced via `X.Instance` by | Present in scene(s) |
|---|---|---|---|---|
| `Weapons/WeaponAmmoManager.cs` | Yes | **Yes** | `Managers/UpgradeManager.cs` | Via `Temp -Player.prefab` → `RoguelikeMode`, `SohamSceneNew`, `Test Scenes/CasualMode`, `Test Scenes/DungeonGeneratorTest`, `Test Scenes/HardcoreMode`, `Test Scenes/ShootTest`. Not in `Main menu` or `BossRoom1Scene`. |
| `UI/StatTracker.cs` | Yes | No | none found | Via `Player Canvas HardcoreMode.prefab` → `BossRoom1Scene`, `RoguelikeMode`, `SohamSceneNew`, `Test Scenes/HardcoreMode`. |
| `UI/CustomCrosshair.cs` | Yes | **Yes** | none found | Direct: `RoguelikeMode`, `SohamSceneNew`, `soham scene new`. Not in `Main menu`/`BossRoom1Scene`. |
| `PlayerController/ActivateEnemies.cs` | Yes (`OnDestroy` nulls Instance) | No | `Enemy/EnemyMovement.cs` | Via `Temp -Player.prefab` (only that prefab is actually instanced anywhere; `Temp -Player BossBattle1.prefab` is unused by any scene). |
| `Managers/MusicManager.cs` | Yes | **No** | none found | Direct: `RoguelikeMode`, `SohamSceneNew`, `soham scene new`. Doesn't persist → music restarts fresh each time this scene loads. |
| `Managers/RoguelikeManager.cs` | Yes | **No** | referenced indirectly via `GameManager.cs`, `BossSceneManager.cs`, `MainMenu.cs` | Direct: `RoguelikeMode`, `SohamSceneNew`, `soham scene new` only. |
| `Managers/GameManager.cs` | Yes | **Yes** | `Enemy/Enemy.cs`, `Managers/BossSceneManager.cs`, `Managers/RoguelikeManager.cs`, `UI/MainMenu.cs`, `UI/OptionsMenu.cs`, `UI/RetryButton.cs` | **Only** `Main menu.unity`. Every other scene relies on it persisting via DontDestroyOnLoad. |
| `Managers/AchievementManager.cs` | Yes | **No DontDestroyOnLoad anywhere in the file** | `Enemy/EnemyKillTracker.cs`, `Managers/AchievementMenuManager.cs`, `Managers/RelicManager.cs` | **Only** `Main menu.unity`. Since it doesn't persist, `AchievementManager.Instance` is **null in every gameplay scene**. See Section 5. |
| `CameraTouch/CameraLead.cs` (class `CinemachineCursorLead`) | Yes | **Yes** | `Weapons/WeaponVFXHandler.cs` | On `Prefabs/CinemachineCamera.prefab` → `RoguelikeMode`, `RoguelikeModeEmpty`, `SohamSceneNew`. Not in `Main menu` or `BossRoom1Scene` directly, but persists into `BossRoom1Scene` at runtime from `RoguelikeMode`. |
| `PlayerController/CoinManager.cs` | Yes | **Yes** | `Enemy/Coin.cs`, `Managers/StoreManager.cs`, `Managers/UpgradeButtonUI.cs`, `Managers/UpgradeManager.cs`, `PlayerController/PlayerHealth.cs`, `PlayerController/PlayerStompController.cs`, `UI/CoinUI.cs` | Direct: `RoguelikeMode`, `SohamSceneNew`, `soham scene new`. Not in `Main menu`/`BossRoom1Scene`. |
| `Managers/UpgradeManager.cs` | Yes | **Yes** | `Managers/RoguelikeManager.cs`, `Managers/StoreManager.cs`, `Managers/UpgradeButtonUI.cs`, `PlayerController/PlayerConeShooter.cs`, `PlayerController/PlayerController.cs`, `PlayerController/PlayerStompController.cs`, `Weapons/WeaponAmmoManager.cs`, `Weapons/WeaponInventory.cs` | Direct: `RoguelikeMode`, `Test Scenes/CasualModeMenu`, `Test Scenes/DungeonGeneratorTest`, `SohamSceneNew`, `soham scene new`. Not in `Main menu`/`BossRoom1Scene`. |
| `PlayerController/PlayerController.cs` | Yes | **Yes** | none found — dead singleton reference (nothing calls `PlayerController.Instance`) | Direct: `RoguelikeMode`, `Test Scenes/CasualMode`, `Test Scenes/HardcoreMode`, `Test Scenes/DungeonGeneratorTest`, `SohamSceneNew`, `soham scene new`. Not in `Main menu`/`BossRoom1Scene`. |
| `Weapons/WeaponUnlockManager.cs` | Yes (logs a warning) | No (rides along — same GameObject as `WeaponAmmoManager`, which does call it) | `Managers/UpgradeManager.cs`, `Weapons/WeaponInventory.cs` | Same GameObject as `WeaponAmmoManager` in `Temp -Player.prefab`. |
| `Enemy/EnemyKillTracker.cs` | Yes | **Yes** | `Enemy/Enemy.cs`, self, `Enemy/Ranged_Enemy/TriangleEnemy.cs` | Direct: `RoguelikeMode`, `SohamSceneNew`, `soham scene new`. |
| `CameraTouch/CameraRecoil.cs` | Yes | No (rides along — same GameObject as `CameraLead`) | none found | `Prefabs/CinemachineCamera.prefab`. |
| `Enemy/JumpFloodPathfinding.cs` | Yes (`Destroy(this)`, component only) | No | none found | `Prefabs/Enemies/Enemy 2.prefab` / `Enemy 3.prefab` — enemies appear to be spawned at runtime, not baked into any scene, so static scene presence couldn't be confirmed. |
| `Managers/StageUnlockManager.cs` | — | — | — | **DEAD CODE** (block comment). Direct GUID match only inside the comment, in `Test Scenes/CasualModeMenu.unity` — not a build-enabled scene. |
| `Managers/PersistentDataManager.cs` | — | — | — | **DEAD CODE** (block comment, ~325 lines). Same as above — only inside `Test Scenes/CasualModeMenu.unity`. |
| `CameraTouch/CameraZoomOnSpeed.cs` | **No** — assigns `Instance = this` unconditionally in `Start()`, no dedupe at all | No | none found | `Prefabs/Main Camera.prefab` → `RoguelikeMode` only. |
| `CameraTouch/CameraShake.cs` | Assigns if null, no duplicate-destroy | No | `PlayerController/PlayerStompController.cs`, `UI/DamageIndicator.cs` | `Prefabs/CinemachineCamera.prefab`. |

### Other managers checked (not singletons)

- `Managers/StoreManager.cs` — no `Instance`; calls `UpgradeManager.Instance`, `CoinManager.Instance`.
- `Managers/BossSceneManager.cs` — no `Instance`; calls `GameManager.Instance` directly.
- `Managers/RelicManager.cs` — no `Instance`, no DontDestroyOnLoad, per-run object; calls `AchievementManager.Instance` (null-guarded) in 3 places.
- `Managers/CasualModeMenuManager.cs` — no `Instance`.
- `Managers/CasualModeManager.cs` — **DEAD CODE** (block comment).
- `Managers/TutorialManager.cs` — no `Instance`.
- `Dungeon Generator/New dungeon Generator/DungeonManager.cs` — trivial `Awake()`, sets a static field, no `Instance` singleton.
- `UI/CursorStateManager.cs` — no `Instance`; purely event-driven, see Section 2.

---

## 2. Cursor State

Two independent, uncoordinated scripts mutate `Cursor.visible`/`Cursor.lockState`, and they **conflict**.

**`UI/CursorStateManager.cs`** (24 lines, no singleton, no `DontDestroyOnLoad` of its own):
- Subscribes to `SceneManager.sceneLoaded` in `OnEnable`.
- `HandleSceneLoaded`: compares `scene.name` to `mainMenuSceneName`, which **defaults to `"MainMenu"`** (no space).
  - Menu match → `Cursor.visible = true`, `lockState = None`.
  - Else → `Cursor.visible = false`, `lockState = Locked`.

**`UI/CustomCrosshair.cs`** (158 lines, singleton, `DontDestroyOnLoad`):
- `Start()` checks `SceneManager.GetActiveScene().name == mainMenuSceneName`, which **defaults to `"Main Menu"`** (with a space — matches the real scene file `Assets/Scenes/Main menu.unity` more closely, modulo casing).
- `OnSceneLoaded` re-grabs crosshair/canvas refs and re-applies cursor state on every scene load.
- `SetCursorAndCrosshair`: menu → `visible = true`, `lockState = None`; gameplay → `visible = false`, **`lockState = Confined`** (not `Locked`).
- `OnDestroy` force-sets `Cursor.visible = true`.

**Conflicts:**
1. **Mismatched menu-name strings** — `"MainMenu"` vs `"Main Menu"` — the two scripts will not agree on whether the active scene "is the menu," especially since the actual scene name has yet another casing (`Main menu`).
2. **Different lock modes for gameplay** — `Locked` (CursorStateManager) vs `Confined` (CustomCrosshair). If both are active in the same scene, whichever `sceneLoaded` subscriber fires last wins — order-dependent, nondeterministic.
3. No coordination between the two; both race independently on every scene load.
4. `CustomCrosshair`'s GUID is embedded directly in `RoguelikeMode`, `SohamSceneNew`, `soham scene new` — not `Main menu` or `BossRoom1Scene`. `CursorStateManager`'s GUID wasn't found via direct scene grep (not checked against nested prefabs) — worth confirming in-editor which scenes actually have it.

---

## 3. Build Settings Scenes & Play-Directly Assumptions

From `ProjectSettings/EditorBuildSettings.asset`, only **3 scenes are enabled in the build**:

1. `Assets/Scenes/Main menu.unity`
2. `Assets/Scenes/RoguelikeMode.unity`
3. `Assets/Scenes/BossRoom1Scene.unity`

Everything else listed is present but `enabled: 0` (not shipped): `Test Scenes/SampleScene`, `Test Scenes/DungeonGeneratorTest`, `Test Scenes/PlayerMovemnent`, `Test Scenes/ShootTest`, `Test Scenes/CasualModeMenu`, `Store Scene`, `Test Scenes/CasualMode`, `SohamScene`, `Test Scenes/HardcoreMode`, `RoguelikeModeEmpty`. Scenes that exist on disk but aren't even in `EditorBuildSettings` at all: `Joystick Pack/Examples/Example Scene`, `Scenes/SohamSceneNew`, `Settings/Scenes/URP2DSceneTemplate`, `soham scene new`, and 11 `Assets/_Recovery/0*.unity` files (Unity crash-recovery artifacts — candidates for cleanup, not analyzed further here).

### `Main menu.unity`
Root objects include `Achievement Manager`, `GameManager`, `AchievementMenuManager`, `EventSystem`, a GameObject literally named **`PersistentSceneManager`** (worth inspecting in-editor — name suggests an intended cross-scene bootstrapper, not confirmed further), `Main Camera`, Canvas/UI.
- Creates: **GameManager** (persists), **AchievementManager** (does NOT persist).
- This is the entry point; expected to be missing everything else (Player, Coin, Upgrade, Weapon, Camera rig, etc.) since those spawn once `RoguelikeMode` loads.

### `RoguelikeMode.unity`
Root objects include `Coin Manager`, `Upgrade Manager`, `Roguelike Manager`, `Music Manager`, `Enemy Kill Tracker`, `CrosshairController`, `Dungeon Generator`, plus nested prefabs `Temp -Player.prefab` (PlayerController, WeaponAmmoManager, WeaponUnlockManager, ActivateEnemies), `Player Canvas HardcoreMode.prefab` (StatTracker), `CinemachineCamera.prefab` (CameraLead/Recoil/Shake), `Main Camera.prefab` (CameraZoomOnSpeed).
- **Missing GameManager and AchievementManager.** `RoguelikeManager.cs` calls `GameManager.Instance` — **pressing Play directly on this scene NPEs** unless Main Menu ran first. Achievement calls are null-guarded, so they silently no-op instead of crashing, but achievements never register if this scene is entered directly.

### `BossRoom1Scene.unity`
Root objects: `EventSystem`, `Global Light 2D`, `Walls`/`Ground`/`Square`s, `PlayerSpawnPoint`, `__BossSceneManager__`, `nextscene`, `BulletPool`, `Dungeon Generator`, leftover upgrade-shop UI objects, `Foilage`.
- **Contains none of the tracked singletons** — no Player, Coin, Upgrade, Weapon, Music, KillTracker, Crosshair, StatTracker, GameManager, AchievementManager. **Zero `Camera` components and zero Cinemachine references in this scene file.**
- This scene is purely a "downstream" scene reached only via `BossSceneManager.cs` → `GameManager.Instance` after `RoguelikeMode` has already spun up Player/Coin/Upgrade/Weapon/Camera/EnemyKillTracker.
- **Pressing Play directly on `BossRoom1Scene`**: `GameManager.Instance` is null → NPE in `BossSceneManager.cs` (not null-guarded there). No player, no camera (see Section 4 camera-persistence gap — the object that *would* persist, `CinemachineCamera.prefab`, has no actual render `Camera` component), no HUD/coin/ammo systems. Effectively unplayable without going through Main Menu → RoguelikeMode first.

### `RoguelikeModeEmpty.unity` (not in build)
Root objects: `EventSystem`, terrain/lighting objects, `Dungeon Generator` — no Player, Coin, Upgrade, Weapon, or kill-tracking systems. Only the `CinemachineCamera.prefab` rig is present. Looks like a stripped WIP/test scene; pressing Play here directly NPEs immediately in anything depending on PlayerController/CoinManager/etc.

### Cross-scene dependency chain
`Main menu` creates GameManager (persists) + AchievementManager (does **not** persist — destroyed the moment you leave Main Menu). `RoguelikeMode` creates PlayerController, CoinManager, UpgradeManager, WeaponAmmoManager, WeaponUnlockManager, EnemyKillTracker, CustomCrosshair, ActivateEnemies, StatTracker, and the camera rig — most persist via DontDestroyOnLoad, a few persist only "for free" by riding on a sibling component on the same GameObject (WeaponUnlockManager, CameraRecoil, CameraShake), and a few don't persist at all (MusicManager, RoguelikeManager, CameraZoomOnSpeed, StatTracker, ActivateEnemies). `BossRoom1Scene` creates nothing of its own and consumes everything carried over from `RoguelikeMode`.

---

## 4. Cameras

Counted standard `Camera` components (YAML class `!u!20`, including prefab-`stripped` entries) and Cinemachine references per scene.

| Scene | Standard Camera | Cinemachine | Detail |
|---|---|---|---|
| `BossRoom1Scene.unity` | **0** | 0 | No camera of any kind in this scene's own file. |
| `Main menu.unity` | 1 (plain) | 0 | "Main Camera" — Transform, Camera, AudioListener, UniversalAdditionalCameraData. No tracked scripts attached. Not persistent. |
| `RoguelikeMode.unity` | 2 (both `stripped`, from prefabs) | 1 | `Main Camera.prefab` (plain Camera + `CameraZoomOnSpeed`, not persistent) and `AimCamera.prefab` (no tracked custom scripts, likely Cinemachine package built-ins). Plus `CinemachineCamera.prefab` (vcam, no legacy Camera component) carrying `CameraLead` (**persists**), `CameraRecoil`, `CameraShake`. |
| `RoguelikeModeEmpty.unity` | 0 | 6 | Only `CinemachineCamera.prefab` — no `Main Camera`/`AimCamera` prefab present at all. |
| `SohamSceneNew.unity` | 0 (not deeply verified) | 6 | Same pattern as RoguelikeModeEmpty; recommend manual in-editor check. |
| `Store Scene.unity` (not in build) | 1 (plain) | 0 | "Main Camera". |
| `Test Scenes/CasualMode.unity` | 0 | 1 | No camera confirmed. |
| `Test Scenes/CasualModeMenu.unity` | 1 (plain) | 0 | Shares fileIDs with Main menu's camera — likely a clone of that scene. |
| `Test Scenes/DungeonGeneratorTest.unity` | 1 (`stripped`) | 1 | Prefab-sourced, not resolved further. |
| `Test Scenes/HardcoreMode.unity` | **0** | 1 (mention only) | No render camera in file. |
| `Test Scenes/PlayerMovemnent.unity` | 1 (plain) | 0 | "Main Camera". |
| `Test Scenes/SampleScene.unity` | 1 (plain) | 0 | Shares fileIDs with `URP2DSceneTemplate.unity` — likely cloned from the URP template. |
| `Test Scenes/ShootTest.unity` | 1 (`stripped`) | 0 | Prefab-sourced. |
| `Settings/Scenes/URP2DSceneTemplate.unity` | 1 (plain) | 0 | Default URP template camera, untouched. |

### Key finding: camera persistence gap

Of the three camera-carrying prefabs in `RoguelikeMode.unity` (`Main Camera.prefab`, `AimCamera.prefab`, `CinemachineCamera.prefab`), **only `CinemachineCamera.prefab` persists** (via `CameraLead`'s `DontDestroyOnLoad`, dragging `CameraRecoil`/`CameraShake` along as siblings). But `CinemachineCamera.prefab` is a pure Cinemachine vcam — **it has no actual render `Camera` component.** The real rendering camera lives on `Main Camera.prefab`, which is not persistent and is destroyed on scene unload. Since `BossRoom1Scene` has zero cameras of its own, **there is a real risk it renders with no active output `Camera` at all** — needs an in-editor Play-mode check before any refactor touches camera/persistence code.

---

## 5. RunStats vs PersistentStats

### `Managers/PersistentDataManager.cs` — **DEAD CODE** (entire file commented out)

Despite the name, this class doesn't compile. Its intended `GameSessionData` fields (defined separately, live, in `Managers/GameSessionData.cs`) were designed as: `currentStageIndex`/`currentLevelIndex`/`totalLevelsCompleted` (progression, persistent-ish), `currentHealth`/`maxHealth` (run-scoped), `coins` (ambiguous), `appliedUpgradeIds`, `damageMultiplier`/`attackSpeedMultiplier`/`moveSpeedMultiplier`/`maxHealthBonus`, `speedUpgradeLevel`/`healthUpgradeLevel`/`pistolDamageBonus`/`MachineGunDamageBonus`/`shotgunDamageBonus`, `isStageTransition` (all run-scoped). None of this is wired to anything live in the build scenes — only consumed by the also-dead `CasualModeManager`/`StageButton`.

**This is the biggest structural gap: no live persistent-data manager exists in the compiled game at all.**

### `Managers/AchievementManager.cs` (live) — the Achievement gap
- `allAchievements` (List<AchievementData>) is correctly PERSISTENT in intent, backed by **PlayerPrefs** (not the dead PersistentDataManager): keys `"TotalCoinsEverCollected"`, `"TotalEnemiesKilled"`, `"UnlockedAchievements"`.
- **Critical wiring gap**: `AchievementManager` never calls `DontDestroyOnLoad` and only exists in `Main menu.unity`. Kills and coins happen in `RoguelikeMode`/`BossRoom1Scene`, so `AchievementManager.Instance` is **null during actual gameplay**. Call sites (`EnemyKillTracker.cs`, `RelicManager.cs`) are null-guarded so nothing crashes, but `CheckKillAchievements()`/`CheckCoinAchievements()`/`OnRelicCollected()` are effectively **never invoked during real play**.
- **Secondary bug**: `"TotalCoinsEverCollected"` is only ever *written* by `RelicManager.cs`, as a side-reward for collecting a duplicate relic — normal coin pickups via `CoinManager.AddCoins()` never touch PlayerPrefs. The coin-total achievement is effectively unreachable through normal play.

### `PlayerController/CoinManager.cs` (live)
- `_currentCoins`, `_coinsCollectedThisRun` (run-scoped, correctly named), `_maxCoins` (run-scoped, grows via upgrades), `_totalCoinsEverCollected` (intended persistent, but **never written to PlayerPrefs** — only "persists" for the app session because the GameObject is DontDestroyOnLoad; resets on restart, and is disconnected from AchievementManager's same-named PlayerPrefs key).
- **Dead reset path**: `ResetForNewRun()` exists but has **zero call sites** anywhere outside its own definition — `RoguelikeManager.cs` never calls it. Because `CoinManager` is DontDestroyOnLoad, run coin totals silently accumulate across multiple play-throughs in one session (e.g. via retry).

### `UI/StatTracker.cs` (live)
No own state — reads `EnemyKillTracker`/`CoinManager` run counters at death time for display. Correctly run-scoped in design, but inherits the accumulation bug above from its data sources.

### `Enemy/EnemyKillTracker.cs` (live)
- `totalEnemiesKilled` — correctly PERSISTENT, round-trips through PlayerPrefs key `"TotalEnemiesKilled"` (shared correctly with AchievementManager's key, though moot while AchievementManager is null during gameplay).
- `killsThisRun` — run-scoped by name, but **same dead-reset bug**: `ResetRunKills()` has zero call sites anywhere. Accumulates across runs in a session since the object is DontDestroyOnLoad.

### `Weapons/WeaponAmmoManager.cs` (live)
`currentAmmoInMagazine`, `isReloading`, `currentWeapon`, `_currentAmmo` — all correctly run-scoped, never touch PlayerPrefs. No persistence bug, though the dictionary also never explicitly clears between runs (low-consequence since it's reinitialized per weapon switch).

### `Weapons/WeaponUnlockManager.cs` (live)
`unlockedWeaponNames` explicitly commented "Per-Run" — but lives on the same GameObject as the DontDestroyOnLoad `WeaponAmmoManager`, so `Awake()` (which clears it) never runs again on retry. **Unlocks accumulate across runs within a session**, contradicting the "Per-Run" design intent.

### `Managers/StageUnlockManager.cs` — **DEAD CODE**. Intended as PlayerPrefs-backed persistent stage-unlock progress; doesn't exist in the compiled game.

### `Managers/UpgradeManager.cs` (live)
- Bonus fields (damage/ammo/speed/stomp/coin-capacity bonuses) explicitly commented "never saved to disk, run-only, ResetRunData() wipes them" — correct by design.
- `CurrentDungeonLevel` — ambiguous/likely bug: never reset by `ResetRunData()`, and since the manager is DontDestroyOnLoad, it climbs across multiple runs in a session instead of restarting at 1, which would corrupt `minDungeonLevel` filtering on a second playthrough.
- **Same dead-reset-call bug**: `ResetRunData()` is only invoked once, from its own `Start()`. No external new-run code path calls it.

### `Managers/RelicManager.cs` (live, NOT a singleton — no `Instance`, no DontDestroyOnLoad)
- `relicsCollectedThisRun`, `uniqueRelicsCollectedThisRun` — correctly run-scoped.
- `isRelicAchievementCompleted` — correctly persistent-derived (read from AchievementManager at Start, subject to the AchievementManager-null gap above).
- **This is the one manager that resets itself correctly** — `Start()` calls `ResetForNewRun()`, and because RelicManager is *not* DontDestroyOnLoad, it's actually recreated (and thus actually reset) each run. This is the pattern the other managers should be mirroring.
- Contains a stray direct `PlayerPrefs.GetInt/SetInt("TotalCoinsEverCollected", ...)` that bypasses `CoinManager` entirely — the only real writer of that key.

### Summary — fields in the wrong place / structural gaps

1. **No live persistence layer**: `PersistentDataManager` and `StageUnlockManager` are dead code. Only scattered raw `PlayerPrefs` calls in `EnemyKillTracker`, `AchievementManager`, and `RelicManager` currently persist anything.
2. **AchievementManager doesn't persist across scenes** — structurally unreachable during gameplay when the events it needs (kills, coins, relics) actually fire. This is the achievement gap.
3. **Coin totals are fragmented**: `CoinManager._totalCoinsEverCollected` (in-memory only) vs the AchievementManager/RelicManager PlayerPrefs key (only written by RelicManager's duplicate-relic path) — two disconnected "lifetime coins" concepts with the same name.
4. **Four "reset per new run" methods are never called from anywhere except their own class's first-time Start/Awake**: `CoinManager.ResetForNewRun()`, `EnemyKillTracker.ResetRunKills()`, `UpgradeManager.ResetRunData()`, `WeaponUnlockManager`'s Awake-only unlock clear. Because all these owning components are DontDestroyOnLoad (or ride along on a DontDestroyOnLoad sibling), run-scoped fields **silently accumulate across multiple runs within the same session**, contradicting their own "this run"/"per-run" naming. `RelicManager` is the sole exception, precisely because it is *not* DontDestroyOnLoad.
5. `UpgradeManager.CurrentDungeonLevel` is never reset, for the same reason.

---

## Open questions to verify in-editor before refactoring

- Does `BossRoom1Scene` actually render anything when reached normally (i.e. is there a runtime-instantiated camera not visible in static scene YAML)?
no there isn't any camera in the scene
- What is actually on the `PersistentSceneManager` GameObject in `Main menu.unity`?
nothing, the script was deleted
- Confirm which scenes have `CursorStateManager` on an active GameObject (not found via direct scene-file GUID grep — may be on a prefab).
the Main menu scene has it, its given to the canvas
- Confirm `SohamSceneNew.unity`'s standard-Camera presence (not deeply verified above).
ignore SohamSceneNew 
