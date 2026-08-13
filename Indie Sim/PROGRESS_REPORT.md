# Architecture Refactor — Progress Report

**Purpose:** session handoff context for Claude Code. Read this before resuming work, alongside `architecture-refactor-plan-v3.md` (Assets/Scripts) and `AUDIT.md` (project root), which remain the source of truth for what each phase is supposed to do.

**Status as of this report:** Phases 1–5 of 8 implemented. Phases 1–3 are committed (`Phase 1 Done`, `Phase 2 Done`, `Phase 3 Done`). **Phase 4, Phase 5, and this session's bugfix round are uncommitted** — sitting as working-tree changes. Commit only when the user asks.

**Branch:** `Sarbo`.

---

## What's been done, phase by phase

### Phase 1 — Bootstrap (built retroactively; plan claimed it was done but nothing existed on disk)
- `Boot.unity` scene, `[Persistent]` root object, `GameSession`/`RunStats`/`PersistentStats` skeletons, `SceneBootstrapGuard`, `PersistentRoot.cs`.

### Phase 2 — Purge & cursor unification
- Deleted confirmed dead code (`PersistentDataManager.cs`, `StageUnlockManager.cs`, `CasualModeManager.cs`, `UI/StageButton.cs`, `UI/CursorStateManager.cs`), `Assets/_Recovery/`, unused prefab/scenes (`RoguelikeModeEmpty`, `SohamSceneNew`, `soham scene new`, unused Temp-Player-BossBattle1 prefab).
- Cursor unification: `CursorController.cs` (under `[Persistent]`) + `SceneUIMode.cs` per-scene marker, replacing string scene-name comparisons. `CustomCrosshair.cs` stripped down to crosshair-rendering only.
- Moved `GameManager` and `AchievementManager` into `Boot.unity` under `[Persistent]` (achievements previously never evaluated during real gameplay — now they do).

### Phase 3 — Camera ownership, `BossArena` renderability, D1 (data-driven boss)
- One persistent render `Camera` + `CinemachineBrain` + `AudioListener` in `Boot.unity`. Removed `Main Camera.prefab`/`AimCamera.prefab` from `RoguelikeMode`. `CinemachineCamera.prefab` made scene-local (removed `CameraLead`'s `DontDestroyOnLoad`).
- Added `CameraTargetBinder.cs` for robust player-finding (poll until found, bind once) — needed because a hand-authored `BossArena` vcam has no baked player.
- Disabled `SceneTransitionHandler.cs` — it was independently fighting `CameraLead` for control of `vcam.Follow` with no defined ordering (BEHAVIOR CHANGE, user-approved).
- **D1:** renamed `BossRoom1Scene` → `BossArena` (all live references fixed, including a separate Unity 6 Build Profile scene list that Phase 1 had missed). Created `BossDefinition.cs` ScriptableObject + `BossDefinition_01` asset. Boss is `MiniBoss1.prefab`; its previously scene-only-override tuned stats (health, speed, bullets, collider, rigidbody, scale) are now baked into the prefab itself. `BossSceneManager` spawns from the `BossDefinition` asset instead of a scene-baked instance.
- **Real bugs found and fixed along the way:**
  - `GameManager.gameScene` was `"RoguelikeScene"` — matched no real scene file.
  - A hand-authored Prefab Variant used the reserved fileID `100100000` for its own internal object, causing an `InvalidCastException` on `Instantiate` — abandoned that approach, edited the base prefab directly instead.
  - Discovered `SceneManager.LoadScene` triggered from inside another object's `Awake()` **never** completes before the current scene's own `Awake`/`Start` pass finishes — no bootstrap hook can beat this. Fixed by making `Camera.main`-dependent scripts (`PlayerConeShooter`, `SimplePlayerRotation`, `CameraLead`) resolve lazily instead of caching once. `SceneBootstrapGuard` ended up as a static `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` class, editor-only.

### Phase 4 — `GameSession` data model, `SaveSystem`, PlayerPrefs migration
- Populated `RunStats`/`PersistentStats` fields per the plan's three-scope table.
- `SaveSystem.cs` — JSON at `Application.persistentDataPath`. `Load()`/`Save()` are **static** (avoids the same Awake-ordering trap found in Phase 3).
- One-time, idempotent PlayerPrefs migration (`TotalEnemiesKilled`, `UnlockedAchievements`, `TotalCoinsEverCollected`) — confirmed working by user (JSON file has sane values, migration log appeared).
- Resolved the two competing "lifetime coins" trackers — `CoinManager.AddCoins()` is now the sole writer of `GameSession.Persistent.TotalCoinsEverCollected`.
- `RelicManager`'s direct `PlayerPrefs` write removed, routed through `GameSession` (BEHAVIOR CHANGE).
- `AchievementManager` rewritten to hold **no local unlock state** — queries `GameSession.Persistent.UnlockedAchievementIds` live (BEHAVIOR CHANGE).
- `EnemyKillTracker` rerouted off `PlayerPrefs` onto `GameSession.Persistent`.
- `GameSession.StartNewRun()` / `EndRun(bool)` added (guarded against double-invocation).

### Phase 5 — Run lifecycle funnel, Demo Complete screen, input gating
- **Funnel lives on `GameManager`**, not `RoguelikeManager` — `RoguelikeManager` is scene-local to `RoguelikeMode` only, but `RetryRun`/`ReturnToMainMenu`/`CompleteRun` must work from `BossArena` too.
- Five funnel methods: `StartNewRun`, `RetryRun` (delegates to `StartNewRun`), `ReturnToMainMenu`, `AdvanceToBoss`, `CompleteRun`.
- Reset bridge (four orphaned reset methods: `CoinManager`, `EnemyKillTracker`, `UpgradeManager`, `WeaponUnlockManager`) added to `GameSession.StartNewRun()` per the plan, marked `TEMP` for Phase 6 removal. Also fixed `UpgradeManager.ResetRunData()` silently never resetting `bonusStompDamage` or `CurrentDungeonLevel`.
- **Rewired five independently-implemented scene transitions** into the funnel: `RetryButton`, `PlayerHealth.GoToMainMenu` (a sixth independent implementation AUDIT.md missed), `MainMenu`'s Play button (previously never called `StartNewRun()` at all), `OptionsMenu` (×2), `Teleporter`, `RoguelikeManager`, `BossSceneManager`.
- Built the Demo Complete screen — hand-authored Canvas/Panel/TMP/Button directly in `BossArena.unity`, cloned from verified-correct existing component blocks in the same file to minimize hand-YAML risk.
- Added `SetGameplayInputEnabled(bool)` gating across `PlayerController`/`PlayerConeShooter`/`PlayerStompController` — confirmed via full reads that **no gating existed anywhere before this**, contradicting the plan's assumption of pre-existing "scattered flags."
- Wired `BossEnemy.OnDeath → BossSceneManager.OnBossDefeated → GameManager.CompleteRun()` — previously **nothing** called `OnBossDefeated()` except its own debug menu item.
- Investigated the plan's "missing pistol" concern via static analysis of `WeaponUnlockManager`/`WeaponAmmoManager`/`WeaponInventory`; found no code path that would lose the pistol unlock on retry. Unconfirmed whether the user has actually observed this — see Known Issues.

### Post-Phase-5 bugfix round (this session, in response to user testing)
- Fixed player staying dead after Retry (`PlayerHealth.ResetForNewRun()`, wired into the `GameSession` reset bridge) — root cause: player is still `DontDestroyOnLoad`, so the same dead GameObject survives a scene reload instead of being replaced.
- Fixed `DemoCompleteScreen` "not found" error — its root Canvas GameObject had `m_IsActive: 0` in the scene file (toggled off during editor testing); fixed back to `1` and made the `FindFirstObjectByType` lookup defensive (`FindObjectsInactive.Include`).
- Fixed stale `deathUIPanel` reference: it was wired as a direct object reference only inside `RoguelikeMode.unity`'s scene override, with no `BossArena` equivalent — so it goes stale the moment RoguelikeMode's canvas unloads. `PlayerHealth` now re-acquires it on every scene load via the `RetryButton` component.
- Added extensive debug logging through the full retry/complete-run chain (`RetryButton`, `GameManager`, `GameSession`, `PlayerHealth`, `BossSceneManager`) at the user's request, to make the next test pass diagnosable from console output alone.

**None of the three bugfixes above have been re-tested by the user yet** — see Known Issues.

---

## Known Issues

### Untested fixes (need verification next session)
1. **Player stays dead after Retry/ReturnToMainMenu** — fix applied (`PlayerHealth.ResetForNewRun()`), not yet re-tested.
2. **Demo Complete screen "not found"** — fix applied (`m_IsActive` correction + defensive lookup), not yet re-tested.
3. **No Retry UI when dying in BossArena** — fix applied (`deathUIPanel` re-acquisition via `RetryButton`), not yet re-tested. Worth a visual check too — the fix targets the `RetryButton`'s own GameObject as the panel to activate, identified by component co-location (it shares a GameObject with `StatTracker`) rather than direct visual confirmation of the full nested-prefab hierarchy.

### New issues reported, not yet investigated
4. **Startup lag/freeze (~0.5–1s)** when entering play from Main Menu or directly from RoguelikeMode — a brief freeze, then RoguelikeMode unfreezes and becomes playable. Not yet diagnosed. Possible suspects worth checking first: `SceneBootstrapGuard`'s additive `Boot` load + `UnloadSceneAsync`, or something in dungeon generation on scene start — pure speculation, needs profiling or console timing logs, not yet done.
5. **"PlayerCanvasRoguelikeMode" appears disabled** on entering RoguelikeMode. Not yet investigated — unclear if this is a symptom of the same stale-reference bug class already found twice this session (Phase 3's camera race, this round's `deathUIPanel`), or something unrelated. Worth checking whether anything explicitly disables this canvas, or whether it's an editor-state artifact like the `DemoCompleteCanvas` `m_IsActive` toggle found above.

### Architectural debt flagged by the user
6. **The (shared) player canvas has accumulated too many dead/unused GameObjects across unrelated systems**, and is shared/reused across scenes in a way that's already caused real bugs this session (the `deathUIPanel` stale-reference bug came directly from this canvas's cross-scene reuse pattern). The user wants **separate canvases per system** (death UI, store/upgrades, HUD, etc.) instead of one shared canvas prefab carrying everything. Not scoped into any existing phase — worth deciding whether this happens as part of Phase 6 (which already touches player/canvas lifecycle via `PlayerSpawner`) or as its own cleanup pass. Flagging here rather than guessing.

### Carried over from Phase 5, still unresolved
7. **"Missing pistol" on retry** — plan's AUDIT.md flagged this speculatively. Static analysis found no code path that would cause it (the unlock list only grows; duplicate-instance guards look correct). Never confirmed by the user as an actually-observed symptom. Ask before chasing further.

---

## Where to resume

Per `architecture-refactor-plan-v3.md`, **Phase 6 (de-persist the managers and the player)** is next — it removes `DontDestroyOnLoad` from `CoinManager`/`UpgradeManager`/`EnemyKillTracker`/`WeaponAmmoManager`/`PlayerController`/`CustomCrosshair`, deletes the Phase 5 TEMP reset-bridge calls, and adds `PlayerSpawner` to instantiate+rehydrate the player fresh per scene. This would also structurally obsolete most of this session's death/retry patches (`PlayerHealth.ResetForNewRun()`, the `deathUIPanel` re-acquisition hack) — a fresh player instance per scene wouldn't carry stale state or stale references in the first place. Worth keeping in mind: some of tonight's fixes are band-aids over the exact problem Phase 6 is designed to solve properly.

Before starting Phase 6, the plan's own checkpoint applies: "Play the game for a while" — the three untested fixes above should be verified first, since Phase 6 will touch the same player lifecycle code and compound any remaining confusion if they're still broken.
