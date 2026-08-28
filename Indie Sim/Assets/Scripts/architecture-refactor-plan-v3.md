# Architecture Refactor Plan v3 — Roguelike Dungeon Project

**Supersedes:** `architecture-refactor-plan.md` (v1) and `architecture-refactor-plan-v2.md` entirely. This is the single source of truth.
**Inputs:** `AUDIT.md` (Phase 0 output, complete). Phase 1 complete — `Boot.unity`, `[Persistent]` root, `GameSession` skeleton, `SceneBootstrapGuard`.
**Status:** Phases 2–8 remain. All previously open architectural questions are now decided (see Decisions section) — there is nothing left to guess at.
**Audience:** Claude Code, operating inside this Unity project.

---

## Ground rules — binding for every phase

1. **One phase per session/commit.** Finish a phase, verify against its acceptance criteria, stop, report back. Do not chain phases.
2. **No balancing work.** No weapon damage numbers, no enemy scaling values, no drop rates, no spawn counts. Structural only.
3. **Preserve behavior unless a phase explicitly says otherwise.** Where a phase intentionally changes behavior it is labelled **BEHAVIOR CHANGE** and states the new behavior. Everything else is relocation, not redesign. If a manager currently does something questionable, replicate it in the new location and flag it in the report rather than silently improving it.
4. **Read any script in full before modifying it.** Summarize what it currently does in the report before changing it. Do not infer responsibility from a class name.
5. **After each phase, list every file created, moved, modified, or deleted.**
6. **If a phase surfaces a design decision not specified here, stop and ask.** Do not guess ownership.
7. **`AUDIT.md` is the reference for what exists.** If reality disagrees with the audit, stop and report the discrepancy before proceeding — do not silently reconcile.

---

## Core architectural principle

> **Persistent objects own no run-scoped mutable state. Scene-local objects own no data that must outlive their scene.**
>
> Run-scoped *data* lives in `GameSession.CurrentRun` — a persistent object with exactly one reset path. Run-scoped *behavior* lives in scene-local managers that read and write that data and are freely destroyed and recreated.

### Why this is the principle

`AUDIT.md` §5.4 found four `Reset…ForNewRun()`-style methods with **zero external call sites**: `CoinManager.ResetForNewRun()`, `EnemyKillTracker.ResetRunKills()`, `UpgradeManager.ResetRunData()`, and `WeaponUnlockManager`'s Awake-only unlock clear. Every owning component is `DontDestroyOnLoad` (or rides on a DDOL sibling), so `Awake`/`Start` never re-runs and those resets never fire. Coins, kills, weapon unlocks, and `UpgradeManager.CurrentDungeonLevel` silently accumulate across runs within a session.

`RelicManager` is the **only** manager that resets correctly — precisely because it is *not* `DontDestroyOnLoad`. It is recreated per run, so its `Start() → ResetForNewRun()` actually executes.

The root cause is that `DontDestroyOnLoad` was used as a substitute for lifetime management. The fix is not to add more reset calls; it is to make lifetimes express intent. Any earlier instruction to move managers *under* `[Persistent]` is void.

**Test for `[Persistent]` membership:** does this component hold mutable state that would be wrong if not reset between runs? If yes, it is either `GameSession` itself or it does not belong under `[Persistent]`.

### Three lifetime scopes

The dungeon clears in place inside `RoguelikeMode` — no scene change — which makes a two-bucket run/persistent model one bucket short.

| Scope | Lifetime | Reset trigger | Owner | Examples |
|---|---|---|---|---|
| **Persistent** | Across app launches | Never, except an explicit erase-save | `GameSession.Persistent` + `SaveSystem` | Achievements, lifetime kills, lifetime coins, total runs, best run |
| **Run** | Main Menu → death or demo completion. **Survives scene loads.** | `GameSession.StartNewRun()` — one call site | `GameSession.CurrentRun` | Coins this run, applied upgrades, relics held, weapon unlocks, dungeon level, run timer, current health |
| **Level** | One dungeon generation, or the boss encounter | Dungeon clear / regenerate | Scene-local objects — **not** `GameSession` | Live enemies, dungeon geometry, spawners, room state, pickups on the floor |

Level scope is expressed by objects being destroyed and rebuilt, never by a stats class. If you find yourself wanting a `LevelStats`, stop and ask — it usually means something is persisting that should not be.

---

## Decisions (previously open, now fixed)

### D1 — One boss scene, data-driven

A single `BossArena` scene, reused for all bosses. Boss selection is data: a `BossDefinition` ScriptableObject (boss prefab, optional arena-variant prefab, music, arena bounds) referenced from `GameSession.CurrentRun`. The current boss becomes `BossDefinition_01`. Do not create a second boss during this refactor — only the slot it will drop into.

### D2 — Boss defeat ends the run: demo-complete state

There is **no loop back** from `BossArena` to `RoguelikeMode`. Defeating the boss is a terminal, successful run end: fold results into `PersistentStats`, save, show a "Demo Complete" screen, and from there the only exit is Main Menu.

Consequences that shape the phases below:

- Run state must survive `RoguelikeMode → BossArena` but never needs to travel the other way.
- The lifecycle funnel has a **terminal success path** (`CompleteRun()`) distinct from the failure path (death) and the abort path (quit to menu). All three end the run; only one shows the demo screen.
- Dying *in* `BossArena` and hitting Retry restarts the whole run from a fresh `RoguelikeMode` — there is no boss-only retry.
- `EndRun()` must be idempotent-guarded: death and boss-defeat both route into it, and a double-call would double-count lifetime stats.

### D3 — Enemy scaling lives in the enemy spawner, unfinished, deferred

It is partially implemented inside the enemy spawner and will be built out later. **Do not build, complete, or tune it in this refactor.** Do two structural things only, in Phase 7: make the spawner read `GameSession.CurrentRun.CurrentDungeonLevel` instead of tracking its own level counter, and have it subscribe to `GameEvents.OnDungeonCleared` instead of being poked directly. That leaves a clean surface for the real work later without doing any of it now.

### D4 — Player is spawned per scene and rehydrated from `GameSession`

The player is currently `DontDestroyOnLoad` via `WeaponAmmoManager`, and that is load-bearing: `BossArena` has a `PlayerSpawnPoint` but no player, so the only reason a player exists there is that the object rode in from `RoguelikeMode`.

New model: **both gameplay scenes instantiate the player prefab at their own `PlayerSpawnPoint` and rehydrate health, weapons, and upgrade bonuses from `GameSession.CurrentRun`.** This makes each gameplay scene independently playable, and removes the last excuse for the player to be persistent. It is the largest behavior change in the plan and gets its own phase.

---

## Target ownership map

End state. Phases move toward it incrementally — do not attempt it in one pass.

### Under `[Persistent]` (in `Boot.unity`)

| Object | Holds run state? | Notes |
|---|---|---|
| `GameSession` | Yes — **the sole exception** | Owns `CurrentRun` + `Persistent`. One reset path. |
| `SaveSystem` | No | JSON to `Application.persistentDataPath` |
| `GameManager` | No (verify) | Scene-loading service. Moves here from `Main menu` in Phase 2. |
| `AchievementManager` | No, after Phase 4 | Pure listener; state lives in `GameSession.Persistent`. |
| `CursorController` | No | Driven by `sceneLoaded`; replaces both current cursor scripts. |
| Render `Camera` + `CinemachineBrain` + `AudioListener` | No | The only ones in the project. |
| `MusicManager` | No | Optional; currently restarts on every scene load. |

### Scene-local

`RoguelikeManager`, `CoinManager`, `UpgradeManager`, `RelicManager`, `StoreManager`, `EnemyKillTracker`, `BossSceneManager`, `StatTracker`, `WeaponAmmoManager`, `WeaponUnlockManager`, `ActivateEnemies`, `DungeonManager`, enemy spawner, all Cinemachine vcams, all UI canvases, **and the player** (per D4).

### Deleted

`PersistentDataManager.cs`, `StageUnlockManager.cs`, `CasualModeManager.cs`, `UI/StageButton.cs` (all fully block-commented dead code), `UI/CursorStateManager.cs`, `Temp -Player BossBattle1.prefab` (referenced by no scene), `Assets/_Recovery/*.unity` (11 crash artifacts), `SohamSceneNew.unity` / `soham scene new.unity` (confirmed ignorable), `RoguelikeModeEmpty.unity`.

---

## Phase 2 — Purge, cursor unification, persistent-root correctness

**Why first:** every later phase greps for `.Instance.`, `DontDestroyOnLoad`, and manager references. Roughly 400 lines of fully commented-out code currently pollute those searches and have already caused one round of confusion. Deleting it costs nothing and makes every subsequent phase cheaper and more accurate.

**Tasks**

1. Delete the dead-code files listed above. Confirm each is fully block-commented and unreferenced before deleting. Delete `Assets/_Recovery/`, the unused player prefab, and the dead scenes. Report anything that turns out to be live.
2. **Cursor unification.** Delete `UI/CursorStateManager.cs`; strip all cursor logic from `UI/CustomCrosshair.cs` so it renders the crosshair and nothing else. Create `CursorController` under `[Persistent]`, subscribed to `SceneManager.sceneLoaded`. **Do not compare scene names as strings** — the live bug is three spellings of one scene (`"MainMenu"`, `"Main Menu"`, actual `Main menu`). Use a build-index or `SceneAsset` reference, or a small per-scene component carrying `[SerializeField] bool showCursor` that the controller reads. Menu → `visible = true, lockState = None`. Gameplay → `visible = false, lockState = Confined` (matching `CustomCrosshair`'s current shipping behavior, not `CursorStateManager`'s `Locked`).
3. **Move `GameManager` from `Main menu.unity` into `Boot.unity` under `[Persistent]`.** Phase 1's `SceneBootstrapGuard` loads `Boot`, but `GameManager` lives in `Main menu`, so pressing Play directly in `RoguelikeMode` still NPEs on `GameManager.Instance`. This closes that gap. Keep its `DontDestroyOnLoad` for now; the `[Persistent]` root makes it redundant later.
4. **BEHAVIOR CHANGE — move `AchievementManager` into `Boot.unity` under `[Persistent]`.** It currently exists only in `Main menu` with no `DontDestroyOnLoad`, so `AchievementManager.Instance` is null throughout gameplay and no achievement has ever evaluated during a real run. Moving it makes them start firing. Do not touch its internals or PlayerPrefs usage — that is Phase 4. Expect dormant code paths to execute for the first time; report anything that throws.

**Acceptance criteria**

- Project compiles; no new warnings.
- Grepping `DontDestroyOnLoad` and `.Instance` returns live code only.
- Cursor is visible in Main Menu and hidden in gameplay on every entry path, with exactly one script touching `Cursor.*`.
- Pressing Play directly in `RoguelikeMode.unity` no longer NPEs on `GameManager.Instance`.
- Achievement evaluation demonstrably runs during gameplay (temporary log, removed before commit).

**Checkpoint:** stop, report, wait.

---

## Phase 3 — Camera ownership and `BossArena` renderability

**Why here:** `AUDIT.md` §4 flags a probable show-stopper. The only camera prefab that persists (`CinemachineCamera.prefab`) is a pure vcam with **no render `Camera` component**; the real render camera (`Main Camera.prefab`) does not persist; and the boss scene contains zero cameras of any kind. It may currently render with no active output camera. You must be able to see the boss arena to verify Phases 5–7, and this work is otherwise independent — a safe, self-contained session.

**Tasks**

1. **Verify before changing anything.** Enter the boss scene through normal flow in Play mode and report whether anything renders and which `Camera` is active.
2. Create one render `Camera` + `CinemachineBrain` + `AudioListener` under `[Persistent]` in `Boot`. These are the only ones in the project from here on.
3. Remove the plain `Camera` from `Main menu.unity`. If the menu canvas is Screen Space Overlay it needs no camera; otherwise point it at the persistent camera.
4. Remove `Main Camera.prefab` and `AimCamera.prefab` from `RoguelikeMode`. `CameraZoomOnSpeed` (currently on `Main Camera.prefab`, and the one singleton in the project with **no duplicate guard at all** — it assigns `Instance = this` unconditionally in `Start()`) moves to the persistent camera or a scene-local vcam as appropriate. Add a duplicate guard while you are there.
5. Make `CinemachineCamera.prefab` **scene-local** and remove `CameraLead`'s `DontDestroyOnLoad`. Its siblings `CameraRecoil` and `CameraShake` currently persist only by riding on the same GameObject; that accidental persistence disappearing is correct and intended. Place a follow-player vcam in `RoguelikeMode` and an arena-appropriate vcam in the boss scene.
6. **Add `CameraTargetBinder`** — a small scene-local component that finds the player at runtime and assigns the vcam's `Follow`/`LookAt`. Required because the player is not present in the scene at author time today (it arrives via DDOL) and will be runtime-spawned after Phase 6. Write it so it works in both worlds: poll or subscribe until a player exists, then bind once.
7. **D1 setup.** Rename `BossRoom1Scene` → `BossArena`. Update Build Settings and every reference. Create a `BossDefinition` ScriptableObject type and one asset, `BossDefinition_01`, wrapping the existing boss. Add a `BossDefinition` field to `BossSceneManager` and have it spawn from that asset rather than from anything scene-baked. **Do not create a second boss.**

Note: the audit found the boss scene contains no managers, no cameras, and little beyond geometry. If repairing it costs more than rebuilding the non-geometry parts, rebuild them — there is nothing there worth preserving for its own sake.

**Acceptance criteria**

- Exactly one enabled `Camera` and one `AudioListener` at runtime at every point in the flow. Duplicate-`AudioListener` console warnings appear immediately if this is wrong — watch for them.
- Main Menu → RoguelikeMode → BossArena → Main Menu with no black frames, no camera pop, no duplicate-camera warnings.
- Pressing Play directly in `BossArena` renders the arena. A missing player is acceptable at this stage; a black screen is not.
- The boss spawns from `BossDefinition_01`, not from a scene-baked object.

**Checkpoint:** stop, report, wait.

---

## Phase 4 — Lifetime model, `GameSession` data, `SaveSystem`

**Why here:** the foundation for Phases 5 and 6. Neither the lifecycle funnel nor de-persisting the managers can proceed until run data has a correct home.

**Tasks**

1. Populate the POCOs created in Phase 1, following the three-scope table:
   - **`RunStats`** — coins this run, coins collected this run, max coin capacity, kills this run, relics held, unique relics this run, applied upgrade IDs, all upgrade bonus fields (damage/ammo/speed/stomp/coin-capacity), unlocked weapons this run, `CurrentDungeonLevel`, dungeons cleared this run, run timer, current/max health, selected `BossDefinition`.
   - **`PersistentStats`** — total coins ever collected, total enemies killed, unlocked achievement IDs, total runs, best run, demo completed flag.
2. `SaveSystem`: JSON to `Application.persistentDataPath`. Loads `PersistentStats` in `GameSession.Awake()`, exposes `Save()`. No auto-save triggers yet.
3. **PlayerPrefs migration — carefully, once.** Three live keys move into `PersistentStats`: `"TotalEnemiesKilled"`, `"UnlockedAchievements"`, `"TotalCoinsEverCollected"`. On first load, if no JSON save exists but PlayerPrefs keys do, import them, write the JSON, set a migration flag. After migration **no script may call `PlayerPrefs` directly**.
4. **Resolve the two competing "lifetime coins."** `CoinManager._totalCoinsEverCollected` is in-memory only and never written to disk. The identically-named PlayerPrefs key is written *only* by `RelicManager`'s duplicate-relic reward path, making the coin achievement unreachable through normal play. Collapse both into `PersistentStats.TotalCoinsEverCollected`, take the larger value during migration, and make normal coin pickup the writer.
5. **BEHAVIOR CHANGE — remove `RelicManager`'s direct `PlayerPrefs` writes.** Route through `GameSession` (Phase 7 converts the call to an event; a direct call is fine until then).
6. **BEHAVIOR CHANGE — `AchievementManager` reads and writes `GameSession.Instance.Persistent` only.** No local copy of achievement state.
7. Add to `GameSession`:
   - `StartNewRun()` — constructs a fresh `RunStats`, increments `Persistent.TotalRuns`.
   - `EndRun(bool completed)` — folds run results into `PersistentStats`, sets `DemoCompleted` when `completed` is true, calls `Save()`. **Guard against double-invocation** (D2: death and boss-defeat both route here). Not called from anywhere yet — Phase 5 wires it.

**Acceptance criteria**

- A save file appears at `Application.persistentDataPath` with correct values and survives a domain reload.
- Existing PlayerPrefs values appear in the new save exactly once; migration is idempotent across repeated launches.
- Grepping `PlayerPrefs` returns hits only inside `SaveSystem`'s migration path.
- `AchievementManager` holds no achievement state of its own.
- Calling `EndRun()` twice in a row increments lifetime stats once.

**Checkpoint:** stop, report, wait.

---

## Phase 5 — Run lifecycle funnel and demo-complete state (closes the P0 list)

**Why here:** with data centralized, one explicit reset path eliminates the whole accumulation bug class and closes the Retry, boss-transition, and upgrade-menu P0s. This phase deliberately does **not** de-persist anything — it builds the funnel and points the existing reset methods at it. Behavior fix first, structural change second, so the risky part stays small.

**The lifecycle, per D2:**

```
Main Menu ──StartNewRun()──> RoguelikeMode
                                  │  (N dungeon clears, in place, upgrades via main canvas)
                                  │
                             AdvanceToBoss()   no reset, CurrentRun intact
                                  ↓
                              BossArena
                                  │
                    ┌─────────────┴─────────────┐
              boss defeated                  player dies
                    │                            │
             CompleteRun()                   (death UI)
        EndRun(completed: true)                  │
                    │                     ┌──────┴──────┐
          Demo Complete screen        RetryRun()   ReturnToMainMenu()
                    │                       │        EndRun(false)
          ReturnToMainMenu()          StartNewRun()       │
                    ↓                       ↓             ↓
                Main Menu            RoguelikeMode    Main Menu
```

**Tasks**

1. Create five methods on `RoguelikeManager` (or `GameManager` if scene loading is its job — decide, and state which in the report):
   - `StartNewRun()` — `GameSession.StartNewRun()`, load `RoguelikeMode`.
   - `RetryRun()` — identical reset, load `RoguelikeMode`. **Wire the broken Retry button to this.** Works identically whether the death occurred in `RoguelikeMode` or `BossArena` (D2: no boss-only retry).
   - `ReturnToMainMenu()` — `GameSession.EndRun(completed: false)`, load `Main menu`.
   - `AdvanceToBoss()` — **no run reset**, sets `CurrentRun.SelectedBoss`, loads `BossArena`, carries `CurrentRun` intact.
   - `CompleteRun()` — `GameSession.EndRun(completed: true)`, show the Demo Complete screen. Does not load a scene; the screen's only exit calls `ReturnToMainMenu()`.
2. **Every** exit path funnels through these. `AUDIT.md` §1 shows `RetryButton.cs`, `MainMenu.cs`, `BossSceneManager.cs`, and `RoguelikeManager.cs` all currently drive transitions independently. After this phase there is exactly one implementation of each.
3. **Demo Complete screen.** A scene-local canvas in `BossArena`, hidden by default, shown by `CompleteRun()`. Content: run summary read from `GameSession.CurrentRun` (kills, coins, dungeons cleared, run time — `StatTracker` already computes most of this at death time, reuse it) plus a single "Main Menu" button. Achievements and the save must complete *before* the screen appears, so lifetime stats shown are accurate. Keep it plain — this is a state, not a feature; polish later.
4. Call the four orphaned reset methods (`CoinManager.ResetForNewRun`, `EnemyKillTracker.ResetRunKills`, `UpgradeManager.ResetRunData`, `WeaponUnlockManager`'s unlock clear) from `GameSession.StartNewRun()` **as a temporary bridge**. Mark each `// TEMP: removed in Phase 6 when this manager becomes scene-local`. Also reset `UpgradeManager.CurrentDungeonLevel` to 1 — it currently climbs across runs and corrupts `minDungeonLevel` upgrade filtering on any second playthrough.
5. **Fix "player not deactivated during upgrade menu."** Because upgrades happen in-scene via the main canvas with no scene change, this is a pause-state problem, not a scene problem. Add a single `SetGameplayInputEnabled(bool)` on `RoguelikeManager` that the upgrade menu's open and close route through, replacing the scattered enable/disable flags. Movement, shooting, dash, and stomp all respect one flag.
6. **Investigate "missing pistol" before treating it as a content bug.** `AUDIT.md` §5 shows `WeaponUnlockManager.unlockedWeaponNames` is documented "Per-Run" but sits on a DDOL sibling, so its `Awake()` clear-and-populate never re-runs. Test specifically whether the pistol is missing only on a *retry* or second run versus a fresh launch. If retry-only, it is this bug and task 4 fixes it. Report findings; do not chase further if it proves to be a genuine prefab issue.

**Acceptance criteria**

- Main Menu → play → die → Retry: the new run starts with 0 coins, 0 kills, no carried upgrades, dungeon level 1, default weapon unlocks. Verify each explicitly, not by feel.
- `AdvanceToBoss()` preserves coins, upgrades, and relics on arrival at `BossArena`.
- Defeating the boss shows the Demo Complete screen with an accurate run summary; lifetime stats are saved before it appears; the only exit reaches Main Menu cleanly.
- Dying in `BossArena` and hitting Retry produces a clean run starting in `RoguelikeMode`.
- Player cannot move, shoot, dash, or stomp while the upgrade menu is open; full control returns on close.
- Grepping `SceneManager.LoadScene` returns hits only inside the funnel methods.

**Checkpoint:** stop, report, wait. **This closes the P0 list.** Play the game for a while before starting Phase 6.

---

## Phase 6 — De-persist the managers and the player

**Why here:** the structural payoff. Phase 5 made resets correct via explicit calls; this makes them correct *by construction*, so the bug class cannot return when a new manager is added later. Safe only because the data already lives in `GameSession`.

**Tasks**

1. Remove `DontDestroyOnLoad` from `CoinManager`, `UpgradeManager`, `EnemyKillTracker`, `WeaponAmmoManager`, `PlayerController`, `CustomCrosshair`. Each becomes scene-local, reading and writing `GameSession.CurrentRun` for anything crossing a scene boundary. **Keep their singleton `Instance` accessors for now** — decoupling is Phase 7, and doing both at once makes the diff unreviewable.
2. Delete the four reset methods and the Phase 5 bridge calls. Reset now happens because the objects are new, exactly as `RelicManager` already works. `GameSession.StartNewRun()` becomes the only reset in the project.
3. `PlayerController.Instance` has zero call sites (`AUDIT.md` §1) — remove the singleton entirely rather than carrying it forward.
4. **D4 — player spawning.** Add a `PlayerSpawner` to both `RoguelikeMode` and `BossArena`:
   - Instantiate the player prefab at that scene's `PlayerSpawnPoint` (`BossArena` already has one; add one to `RoguelikeMode` or have the dungeon generator supply it).
   - **Rehydrate from `GameSession.CurrentRun`** immediately after instantiation and before the first frame of input: current/max health, unlocked weapons, equipped weapon, ammo state, and every upgrade bonus (damage/ammo/speed/stomp/coin capacity).
   - **Write back to `CurrentRun` on scene exit** so nothing is lost across `RoguelikeMode → BossArena`. Alternatively have the player read and write `CurrentRun` continuously as the single source of truth — prefer this if it does not require invasive changes to `PlayerController`. State which approach you took and why.
   - `CameraTargetBinder` from Phase 3 handles vcam binding for the runtime-spawned player. Verify it still binds correctly now that spawn timing has changed.
5. `MusicManager` — decide: move under `[Persistent]` so music does not restart on every scene load, or leave scene-local. Not load-bearing; state the choice.

**Acceptance criteria**

- Grepping `DontDestroyOnLoad` returns hits only under `[Persistent]` in `Boot`.
- Two consecutive runs in one editor session without a domain reload: the second run's starting state is identical to the first's.
- Health, weapons, ammo, and upgrade bonuses all survive `RoguelikeMode → BossArena` intact.
- **Pressing Play directly in `RoguelikeMode` and directly in `BossArena` both produce a playable scene with a player, a camera, and working HUD.** This is the concrete measure of whether the refactor achieved its goal.
- Full flow still works end to end, including the Demo Complete path.

**Checkpoint:** stop, report, wait.

---

## Phase 7 — Event bus and unified enemy death

**Why last:** highest-risk phase, and the one most able to break things quietly. Decoupling a system whose lifetimes are wrong just relocates the bugs — so lifetimes get fixed first.

**Tasks**

1. Create a static `GameEvents` class. Minimum set, based on the audit's actual call graph: `OnEnemyDied`, `OnCoinsEarned`, `OnUpgradeSelected`, `OnRelicAcquired`, `OnDungeonCleared`, `OnBossPhaseChanged`, `OnBossDefeated`, `OnRunStarted`, `OnRunEnded`.
2. **`OnDungeonCleared` carries more weight than it looks.** Because dungeon clears do not change scenes, it is the only signal distinguishing a level boundary from a run boundary. The upgrade menu, dungeon regeneration, the enemy spawner, and the boss-transition counter should all subscribe to it rather than each tracking clears independently.
3. **Unified enemy death — the P1 root cause.** Create one `EnemyDeathHandler` that raises `OnEnemyDied`. Light cleanup, coin drops, stomp state, gun/target state, and `EnemyKillTracker` each subscribe independently. Remove the ad-hoc calls currently made from spawner and enemy code — the audit shows `Enemy.cs` reaching directly into `GameManager.Instance` and `EnemyKillTracker.Instance`; those go away here.
4. **D3 — enemy scaling hooks only, no implementation.** Make the spawner read `GameSession.CurrentRun.CurrentDungeonLevel` rather than tracking its own level counter, and have it subscribe to `OnDungeonCleared` rather than being called directly. **Do not build out, complete, or tune the scaling logic** — leave it exactly as capable as it is now, just correctly wired. Note in the report what shape the remaining work takes.
5. Replace direct cross-manager `.Instance` calls with events. Highest-traffic targets per the audit: `CoinManager` (7 callers), `UpgradeManager` (8 callers), `GameManager` (6 callers). Some `.Instance` uses are legitimate service lookups rather than coupling — flag those for human review instead of forcing them through events.
6. Convert `RelicManager`'s coin reward (Phase 4 step 5) to raise `OnCoinsEarned`.

**Acceptance criteria**

- No manager script calls a *mutating* method on another manager's `Instance`. Remaining `.Instance` uses are read-only lookups, each justified individually in the report.
- One playtest pass in which every enemy death cleans up lights, coins, guns, and stomp state consistently — including deaths by stomp, by each weapon, and during a dungeon transition.
- Adding a new subscriber to `OnEnemyDied` requires touching zero existing files.
- All event subscriptions are unsubscribed in `OnDisable`/`OnDestroy`. **Static events plus scene-local subscribers is exactly the combination that leaks** — audit this specifically, it is the one way this phase can introduce a subtle new bug class while fixing an old one.

**Checkpoint:** stop, report, wait. Expect iteration.

---

## Phase 8 — Verification and summary

**Tasks**

1. **Play-directly test, all four scenes:** `Boot`, `Main menu`, `RoguelikeMode`, `BossArena`. All must run with no manual setup steps.
2. **Full loop:** Main Menu → run → clear dungeons → open and close the upgrade menu → boss → defeat → Demo Complete → Main Menu → second run. Watch cursor, camera, coins, upgrades, and weapon unlocks at every transition.
3. **Failure paths:** die in `RoguelikeMode` → Retry; die in `BossArena` → Retry; quit to menu mid-run. All three produce clean state.
4. Restart the editor entirely; confirm `PersistentStats` survived and `RunStats` did not.
5. Re-run the greps: `DontDestroyOnLoad`, `.Instance.`, `PlayerPrefs`, `SceneManager.LoadScene`, `Cursor.`. Each should return a small, explainable set of hits.
6. Produce `REFACTOR_SUMMARY.md`: what moved where, what is event-driven now, every deferred TODO, and — importantly — the ownership rules from this document restated as a short "where do I put a new manager?" guide for future work.

**Checkpoint:** final review before resuming feature work.

---

## Explicitly out of scope

- **Enemy scaling implementation** (D3) — hooks only in Phase 7; the actual system is later, separate work
- **A second boss** — the architecture supports it after Phase 3; building it is separate
- Weapon damage values (Shotgun/AK47 missing damage) — separate task
- Demo Complete screen polish — Phase 5 builds the state, not the presentation
- Controller support, cooldown indicators (P2)
- Enemy pathfinding, swarming, balancing (P3)
- Asset zoo / weapon test scene — comes after this refactor
- Options menu content, tutorial content
- Cleanup of unused test scenes beyond the Phase 2 purge
