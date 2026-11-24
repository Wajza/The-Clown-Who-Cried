# The Clown Who Cried — Phase 2 Checkpoint: Systems Vertical Slice

**Course:** SWE 402 · **Engine:** Unity 6000.5.9f1 (URP 2D) · **Repository:** `Wajza/The-Clown-Who-Cried` · **Date:** 2026-11-25

**▶ Play the slice:** [The Clown Who Cried on Unity Play](https://play.unity.com/en/games/e31d1144-ad83-414e-82d4-0e443815c874/the-clown-who-cried) (Web build)

---

## 1. Playable Slice Summary

### Start → end boundaries

The slice is one continuous run of the demo. It starts on the **Title screen** and ends on the **End of Demo** screen.

| # | Segment | Scene(s) | Start trigger | End condition |
|---|---------|----------|---------------|---------------|
| 0 | Title | `Title` | Launch the build (Web or Windows) | **New Game** or **Continue** (Continue reads the save) |
| 1 | Opening cutscene | `Tumbleton_PipHouse` | New Game | Timeline ends or is skipped. Plays once and is remembered in the save |
| 2 | Prologue P1 | `Tumbleton_PipHouse` | Starts by itself | Use the mirror, then walk out the right side |
| 3 | Prologue P2–P4 | `Tumbleton` | Talk to Officer Honk, Balloon Betty and Old Tumbles in turn | Cross the bumper-car street (unlocks **Dash**), pop 8 balloons (unlocks **Star Coins**), break 5 pots with a pogo over spikes (unlocks the **Mallet**) |
| 4 | Prologue P5 | `Tumbleton_DoctorOffice` | The doctor's door opens after P4 | Talk to Dr. Mellow (unlocks **Tear Burst**). The station gate opens |
| 5 | Train map | `TrainMap` | Station door | Pick the Wonderwick station |
| 6 | Wonderwick tasks W1–W4 | `Wonderwick_Hub`, `W1_Delivery`, `W2_MirrorMaze`, `W3_Coaster`, `W4_Audition` | Talk to each task giver in the hub | All four tasks done. The Laugh Tower gate opens |
| 7 | Boss | `ChortleArena` | Enter the Laugh Tower. The intro cutscene plays | Chortle's mask reaches 0, then the outro cutscene plays |
| 8 | End | `EndOfDemo` | Boss defeated | Credits end or are skipped, and the game returns to the Title |

### Core loop status

The loop is **talk → get a task → use the abilities you have to beat a platforming, combat or puzzle challenge → earn a new ability or open a gate → move on**. This loop works across all 9 prologue and Wonderwick tasks. Each task gives a new ability or opens the way forward, so the loop also teaches the player the controls one step at a time.

### Win / fail behaviour

| Type | Behaviour | How it is verified |
|------|-----------|--------------------|
| **Win (slice)** | Beating Chortle sets the `chortle_defeated` flag, adds journal page 2 and +1 max Smile Star, and saves **outside** the tower. Then the End of Demo screen loads. Coming back to the arena later shows no boss and an open exit. | Part 8 test (38/38) |
| **Fail: health** | At 0 Smile Stars, Pip waits, the screen fades, she respawns at the last checkpoint or the scene's entry point with full stars, and control comes back. | Part 1 test (36/36), Combat test (45/45) |
| **Fail: hazards** | Spikes and bumper cars cost one star and put Pip back on safe ground. | Combat test, Part 6 test |
| **Fail: task** | W1 has a 2:00 timer. When it runs out, all boxes and stalls reset and the task can be taken again. W4 (rhythm audition) needs 6 hits. Fewer plays the fail dialogue and the player retries. | Part 7 test (73/73) |
| **Repeatable** | Task progress, broken objects, opened gates, abilities and the boss result are all saved. **Continue** brings them back. | Parts 3, 6 and 8 tests |

### Known limitations

- **Audio is placeholder only.** 46 generated `.wav` clips (38 SFX, 8 music) live in `Assets/Audio/Placeholders`. The music and SFX are wired up but are not final assets.
- **Difficulty** (`Cozy / Standard / Showstopper` in `ChortleBoss`) exists in code but has no menu option yet.
- **The world never regains its colour.** `WashedOutView.Depth` is designed to rise after each boss, but the slice has only one boss, so the depth is never raised.
- **No profiler numbers yet** (see §6).
- **Whoopee Wobbler:** the fix for it sinking into the floor has not been confirmed in play yet (§4.3 #2).
- Tilemaps still hold some unused references to deleted placeholder tiles. They are harmless, but a cleanup is planned.

---

## 2. Systems Implementation Status

Legend: ✅ implemented and covered by tests · 🟡 implemented, partial or not yet re-tested · ⬜ planned

### 2.1 Controls and interactions

| System | Status | Implementation |
|--------|--------|----------------|
| Movement and jumping | ✅ | `PlayerController`: Rigidbody2D run, variable-height jump, **coyote time (0.1 s)**, **jump buffering**, faster falling |
| Dash, Mallet, Star Coins, Blue Sight, Tear Burst | ✅ | Each is locked until earned (`PlayerAbilities` + save flags). Up and down aiming for the mallet and coins, plus a pogo bounce |
| Input | ✅ | Input System with two action assets (`PlayerControls`, `SystemControls`). Keyboard, mouse and gamepad. Interact = F or gamepad East, kept apart from Tear Burst (E) |
| Interactions | ✅ | `InteractionInput` picks the **nearest** `Interactable` and shows one prompt. Used for NPCs, the mirror, doors, switches and the stage |
| Input locking | ✅ | `PlayerInputLock` tracks every lock owner, so dialogue, cutscenes, loading and pause cannot unlock each other |
| Controls screen | ✅ | `ControlsScreen` builds its list from the real input assets, so it always matches the bindings |

### 2.2 Physics and collision

| System | Status | Implementation |
|--------|--------|----------------|
| Layers | ✅ | Ground = 6, Player = 7, Enemy = 8. Coins and the mallet only hit the Enemy layer |
| Hit detection | ✅ | Mallet: an overlap check during the swing's active frames. Coins: projectile triggers. Damage goes through the `IDamageable` / `DamageInfo` / `IStunnable` interfaces |
| Pip vs. enemies | ✅ | `EnemyPassThrough` turns off Player–Enemy collision, and `ContactDamage` uses overlaps instead. This stopped enemies from pushing Pip into or through the floor |
| Moving platforms | ✅ | `PlatformCarrier` / `WaypointMover` / `RotatingWheel` carry Pip (the Ferris wheel gondola is tested) |
| Wobbler pounce | 🟡 | Hops are aimed with ballistic maths (`vy = √(2gh)`, `vx = d / t`). It bounces when it lands on Pip from above. Uses continuous collision detection |
| Hazards | ✅ | `PlayerHazards` / `UnsafeGround` send Pip back to the last safe ground |
| Determinism | 🟡 | Movement runs in `FixedUpdate` and the tests use fixed timings. We have not run a frame-rate stress test yet |

### 2.3 AI, combat and challenge logic

All enemies share **`EnemyBase`**, a coroutine-driven state loop. It enforces one fairness rule: **every attack must call `Warn()` first, and the warning lasts at least 0.5 s** (`MinWarningTime`). During the warning the enemy blinks, a warning sign shows, and the `AttackWarned` event fires.

| Agent | Behaviour |
|-------|-----------|
| Whoopee Wobbler | Idle → (Pip within 10 tiles) → wobble warning → pounce hop aimed at Pip → land → rest → repeat |
| Pie Slinger | Wind-up warning → throws Straight, Arc or Alternate. It uses the left- or right-facing pie prefab depending on which side Pip is on |
| Jack-in-the-Box | Rattle warning → spring. `TimedLid` lids open and close on a timer |
| WaveSpawner | Spawns the W3 enemy waves. The task finishes when every wave is cleared |
| **Chortle (boss)** | **An explicit finite state machine** (`ChortleState`). Each state is a coroutine, and `ChangeState()` moves between them:<br>`PhaseIntro` (can't be hurt) → Phase 1 at mask 90–60: `PieVolley ⇄ Panting` (takes extra damage while panting)<br>Phase 2 at mask 60–30: `CloneShow`. Chortle teleports anywhere in the arena, including the platforms, and hides among 4 copies that are the same size as him. **Blue Sight** makes the fakes see-through<br>Phase 3 at mask 30–0: `BombBarrage`. Only Tear Burst does real damage<br>Any time: Tear Burst → `Stunned`. Mask at 0 → `Defeated`. A "!" warning shows before each rubber chicken drops |

The challenge mix includes a timed delivery (W1), a Blue Sight puzzle with fake floors, a secret platform and mirror teleports (W2), a Ferris wheel ride plus switches and waves (W3), and a 100 BPM rhythm audition (W4).

### 2.4 Game-state flow

- **`GameState`**: `Title, Loading, Playing, Paused, Dialogue, Cutscene`. `GameManager` owns the state, and every change is announced through `GameEvents`.
- **`SceneLoader`**: fade out → lock input → load → place Pip at the named `SpawnPoint` → autosave → fade in → unlock input. There is only one `GameManager` at any time (tested).
- **Save system**: `SaveData` stores the scene, spawn point, flags, tasks, abilities, journal pages and fragments. `SaveSystem` writes it to disk, and `Checkpoint` updates the respawn point.
- **Tasks**: `TaskManager`, `TaskGiver`, `TaskGate`, `TaskObjective` and their subclasses (talk, area, signal, breakable). They are data-driven (`TaskData` ScriptableObjects in `Resources/Tasks`).
- **Cutscenes**: Timeline with a custom `DialogueTrack`. `BossFightFlow` runs intro → fight → outro → rewards → End of Demo.

---

## 3. UI and Audio First Pass

### UI / UX feedback

| Element | Status |
|---------|--------|
| HUD: Smile Stars, Tear Gauge, Blue Sight meter (shown only once earned), coins, Star Fragments | ✅ (Part 4: 79/79) |
| Boss bar with a phase label | ✅ |
| Task tracker, with a "New task" banner, counters (e.g. `Pop the balloons (3/8)`) and the W1 timer | ✅ |
| Interaction prompt bubble (one at a time, nearest target) | ✅ |
| Dialogue box with a typewriter effect, speaker names and choices that wrap around | ✅ (Part 2: 59/59) |
| Pause, Settings (music, sound, screen shake, shake amount, hit pause), Controls and Journal (task boards, torn pages) | ✅ |
| Level name card when entering a level | ✅ |
| Combat feedback: hit pause, Cinemachine impulse screen shake, squash and stretch, dust, enemy warning blink and "!" bubble, boss shake | ✅ |
| Locked exits explain themselves ("Pip says something") instead of failing silently | ✅ |

### Audio

| Item | Status |
|------|--------|
| `AudioManager` + `SoundLibrary` (every `SoundId` has a clip, which is tested) | ✅ |
| Per-level music from `LevelInfo`, crossfades, quieter music while paused | ✅ |
| Boss music, final-phase music and a victory sting | ✅ |
| SFX hooks: player (`PlayerSounds`), world (`WorldSounds`), game events (`GameEventSounds`), boss (`BossSounds`). Jumps get slight random pitch | ✅ |
| Mixing | 🟡 Separate music and SFX volumes stored in PlayerPrefs. No AudioMixer yet (volumes are set directly in code; see the `AudioManager` comment) |
| Spatial audio | ⬜ Deliberately 2D (`spatialBlend = 0`). For a single-screen 2D platformer, a panning pass is planned for Phase 3 |
| Final assets | ⬜ All 46 clips are placeholders |

---

## 4. Playtest Report

### 4.1 Setup

| | |
|---|---|
| **Automated playtests** | 11 scripted test scenes in `Assets/Scenes/Tests` (driven by `SystemTestBase`). They play through the real systems, for example pressing K to swing, and write PASS/FAIL lines to `Logs/*TestResults.txt` |
| **Manual playtests** | Internal sessions in the Unity Editor by Wajza, Nov 18–24. Full report: [`playtest_internal_wajza.md`](playtest_internal_wajza.md) (20 findings: 3 P0, 13 P1, 4 P2) |
| **Outside playtesters** | Session script and form: [`playtest_form.md`](playtest_form.md). **In progress:** the first session (Raneem, Nov 24, keyboard and mouse) has its header filled in. Times, deaths and ratings are not recorded yet. The target is 3 players |
| **Build** | Web build on [Unity Play](https://play.unity.com/en/games/e31d1144-ad83-414e-82d4-0e443815c874/the-clown-who-cried) (`Build Profiles/Web.asset`), plus the Windows build and `BuildSmokeTest` (Part 10) |

### 4.2 Automated results (latest log for each suite)

| Suite | Checks | Result | Log date |
|-------|--------|--------|----------|
| Part 1: Core (state, death, respawn, save, scene loading) | 36 | ✅ all pass | Nov 18 |
| Part 2: Dialogue and interaction | 59 | ✅ | Nov 18 |
| Part 3: Tasks | 53 | ✅ | Nov 18 |
| Part 4: UI | 79 | ✅ | Nov 18 |
| Part 5: Levels, camera, abilities | 91 | ⚠️ 6 fail (see below) | Nov 24 |
| Part 6: Prologue P1–P5 | 60 | ✅ | Nov 24 |
| Part 7: Wonderwick W1–W4 | 73 | ✅ | Nov 18 |
| Part 8: Cutscenes and boss flow | 38 | ✅ | Nov 24 |
| Part 9: Audio | 30 | ✅ | Nov 18 |
| Combat | 45 | ✅ | Nov 18 |
| **Total** | **564** | **558 pass / 6 fail** | |

**The 6 Part 5 failures** were `no Pixel Perfect Camera` and `same camera zoom (7)` in three levels. They were caused by the camera work in progress during that run. The fix was to turn off Pixel Perfect when a scene loads and to use a fixed zoom for single-room scenes. The test was updated to expect this fixed-room zoom, but **the suites have not been re-run since then**. Seven of the ten logs are from Nov 18, before the Nov 24 polish commit. The table will be updated after the re-run (§4.4, item 1).

### 4.3 Manual findings and triage

Severity: **P0** blocks progress · **P1** clearly wrong, harms play · **P2** polish

| # | Finding (what we saw) | Sev. | Root cause | Fix | Status |
|---|-----------------------|------|-----------|-----|--------|
| 1 | The camera lost Pip and showed the area outside the background | P0 | Pixel Perfect Camera fighting Cinemachine, plus the wrong confiner | `PipCameraFollow` (no dead zone, hard limits). `FixedCameraRoom` for the bedroom, the office and the arena. `KeepCameraInBounds` for Tumbleton, the hub and W1–W4 | Fixed |
| 2 | The Wobbler sank under the floor | P0 | The Player–Enemy collision pushed the Wobbler through the ground | `EnemyPassThrough`, contact damage by overlap, continuous collision detection | Fixed, needs confirming |
| 3 | Pip dropped lower while throwing and dashing ("dashing in another lane") | P1 | Throw frames had different pivots. The dash squash and stretch squashed her vertically | Feet-planted pivots for each frame. `dashStretch` changed to (1.12, 1) | Fixed |
| 4 | Pip looked like she was falling on moving platforms | P1 | `IsGrounded` flickered on the carrier | `PipAnimator` checks the platform under her feet, with an air grace of 0.08 s | Fixed |
| 5 | Pies always flew left | P1 | A single pie sprite | Left and right pie prefabs chosen by Pip's side (Pie Slinger and Chortle) | Fixed |
| 6 | The mallet animation didn't match the hit | P1 | Animation time wasn't tied to the swing's timing | A `SwingPhase` motion-time parameter driven by the wind-up, active and recovery windows | Fixed |
| 7 | Boss clones were stacked and too small. Chortle couldn't use the platforms | P1 | Clone layout and scale | Scattered spawns at the boss's scale (0.65). Teleports anywhere in the arena | Fixed |
| 8 | Trapdoors and cannons cluttered the arena | P2 | Design change | Removed. A "!" bubble now warns before each chicken drop | Fixed |
| 9 | Blue Sight's blue tint hid the real colours | P2 | Volume settings | Blue Sight now brings back full colour over the washed-out world | Fixed |
| 10 | Doors in single-room scenes felt clunky | P2 | Design | Walk out the right side (`LevelExit`) | Fixed |

### 4.4 Planned fixes and next actions

1. **Re-run all 11 test scenes** on the current `main`, then commit the new logs. *(Owner: Wajza)*
2. Confirm finding #2 in a manual session in W1 and W3, where Wobblers appear most.
3. Finish the outside playtest with **at least 3 players** using [`playtest_form.md`](playtest_form.md) (one copy for each player). Run start → End of Demo, timed for each segment, with deaths, times the facilitator helped, and 1–5 ratings. Then add the findings to §4.3. The first session (Raneem) has started.
4. Clean up the unused tile references in the tilemaps.
5. Set **Company Name** in Project Settings > Player. It is still `DefaultCompany`, which is also the save folder's name.

---

## 5. Production Update

### 5.1 Workflow

- **Branches and PRs:** we work in feature branches and merge to `main` through PRs. So far: `feature/player-combat-boss` (#1), `studentB-core` (#3), `combat-fixes` (#4), `character-art` (#7), and smaller merges for `chortle-animation`, `dash-animation`, `jump-animation`, `mallet-animation`, `pip-animations` and `art-import-fix`.
- **History:** 46 commits on `main` since Nov 13.
- **Documentation:** every script starts with a summary comment that explains its setup. [`README.md`](../../README.md) covers how to play, the controls and the project setup. [`Assets/TEAMMATE_TODO.md`](../../Assets/TEAMMATE_TODO.md) carries the handoff notes: open tasks by owner, and the names other code depends on.
- **GitHub Project board:** it was not used as out main production update, we inform each other in whatsapp
### 5.2 Ownership and completed work

| Member | Area | Delivered in Phase 2 |
|--------|------|----------------------|
| Wajza | Player, combat, enemies, boss, game feel, integration and polish | Player controller and abilities. `EnemyBase`, the enemies and the Chortle FSM. Feel (hit pause, shake, squash and stretch). Animator integration. Camera system. Arena redesign. Demo polish |
| Sarah Almas | Core systems | Parts 1–10: core, save, scene loading, dialogue, tasks, UI, levels, prologue, Wonderwick, cutscenes, audio, Windows build and smoke test, and the automated test scenes |
| Dana Alyahya | Character art and animation | Pip, Chortle, Wobbler and Pie Slinger animations. Environment art and backgrounds |


### 5.3 Carry-over to Phase 3

| Task | Reason it is carried over |
|------|---------------------------|
| Replace the placeholder audio with final music and SFX | Asset production |
| Add an AudioMixer with music, SFX and UI groups, plus ducking during dialogue | First pass used direct volume control |
| Profile the build and fix bottlenecks (§6) | No profiler pass yet |
| Add a Difficulty setting to the menu | The code is ready, the UI isn't |
| Finish the outside playtest round (3+ players) | §4.4, item 3 |
| Review the demo polish commit (`798f003`) as a team | It was committed straight to `main`. Future work goes through branches and PRs again |

### 5.4 Phase 3 readiness

- ✅ The full start → end flow is playable and saved. The win state has been checked by tests and by manual play.
- ✅ 558 of 564 automated checks passed on their latest runs. The 6 failures were an old camera expectation, and the test has since been updated. ⚠️ A re-run on the current `main` is still due.
- ✅ A playable Web build is public on Unity Play.
- ✅ Every core system (input, physics, AI FSM, state flow, UI and audio hooks) is integrated, not stubbed.
- ⚠️ We still need a profiling baseline, an outside playtest and final audio before calling it content-complete.

---

## 6. Competency Coverage

| Competency | How Phase 2 shows it | Phase 3 plan |
|------------|----------------------|--------------|
| **Core gameplay systems** | Abilities unlocked through tasks, `GameState` flow, save and continue, win and fail states (§1, §2.4) | Difficulty modes, more tasks |
| **Physics and collision** | Layer matrix, overlap-based contact damage, pass-through, ballistic hops, platform carrying, coyote time and jump buffering (§2.2) | Frame-rate stress test for determinism |
| **AI behaviour** | `EnemyBase` coroutine state loops with a forced ≥0.5 s warning. Chortle's 7-state FSM across 3 phases (§2.3) | Adjust by difficulty, smarter teleport choice |
| **Real-time graphics** | URP 2D Renderer (`Renderer2D.asset`). Global Volumes: `WashedOutView` (priority 0) and `BlueSightVolume` (priority 10) with Color Adjustments and Vignette. Fixed 16:9 letterbox (`AspectLock`). Sprite import, PPU and pivot standardisation | 2D lights for mood, colour returning after the boss |
| **Technical art and polish** | Animator controllers driven by motion time (mallet `SwingPhase`, jump `AirPose`). Feet-planted pivots. Squash and stretch, dust, screen shake. Cinemachine 3 follow with confiners and fixed-room framing | VFX pass (confetti, hit sparks) |
| **Audio systems** | Data-driven `SoundLibrary`, music by level, crossfades, music states for each boss phase, event-driven SFX (§3) | Mixer, ducking, final assets, stereo panning |
| **UI/UX feedback** | HUD, prompts, tracker, journal, controls screen that builds from the bindings, warnings before every attack, locked exits that explain themselves (§3) | Usability findings from the outside playtest |
| **Performance and debugging** | Debugging: `PlayerDebugLog` (the console explains ignored inputs), automated logs for each suite, root-cause fixes (§4.3), `BuildSmokeTest`. **Profiling evidence: not yet captured** | Unity Profiler capture in the hub and arena (CPU ms, GC alloc per frame, draw calls) in the Windows build, then fix the top bottlenecks |
| **Production practices** | Feature branches, PRs, clear ownership, documented scripts, handoff notes (§5) | Tie every PR to a board issue, use milestones for Phase 3 |

---

## 7. Evidence

- Playable build: [Unity Play](https://play.unity.com/en/games/e31d1144-ad83-414e-82d4-0e443815c874/the-clown-who-cried)
- Test logs: `Logs/Part1TestResults.txt` … `Part9TestResults.txt`, `Logs/CombatTestResults.txt`
- Test scenes: `Assets/Scenes/Tests/*.unity`, scripts in `Assets/Scripts/Tests/`
- Screenshots: `docs/phase2/images/`. Press **F12** in Play Mode to save one (`ScreenshotKey`, editor only). Planned shots: title, a prologue task, W2 with Blue Sight, Chortle's phase 2 clones with Blue Sight on, End of Demo. (It was not provided, u can clone the project and test from the unity diractly)
