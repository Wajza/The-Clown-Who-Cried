# TEAMMATE_TODO

Handoff notes between the team. Last updated **2026-11-25**.

| Who | Area |
|-----|------|
| **Wajza** | Player, combat, enemies, boss, game feel, camera, polish |
| **Sarah** | Core, save, dialogue, tasks, UI, levels, cutscenes, audio, build, tests |
| **Dana** | Character art, animation, environment art |

Status: `[ ]` open · `[x]` done. The code never edits another person's files. Where something is
missing, it uses a TODO plus an event or interface, and it starts working once the other person adds it.

**Play the current build:** [Unity Play](https://play.unity.com/en/games/e31d1144-ad83-414e-82d4-0e443815c874/the-clown-who-cried)

---

## 1. Open now

### Phase 2 checkpoint (`docs/phase2/checkpoint.md`)

- [ ] **Re-run all 11 test scenes on the current `main`** and commit the new `Logs/*TestResults.txt`,
  then update table 4.2 in the checkpoint. *(Wajza)* The logs are still from Nov 18 and 24. The
  Part 5 log still has the 6 old camera failures from before the camera fix.
- [ ] **Confirm in play that the Whoopee Wobbler no longer sinks into the floor** (W1 and W3 have the most Wobblers). *(Wajza)*
- [ ] **Outside playtest: at least 3 players** using `docs/phase2/playtest_form.md`. The first
  session (Raneem, Nov 24) has its header filled in. Its segment table and ratings are still
  empty. Use one copy of the form for each player, then add the findings to checkpoint §4.3. *(everyone)*
- [ ] **Screenshots** (F12 in Play Mode saves them to `docs/phase2/images/`): the title, a prologue
  task, W2 with Blue Sight, Chortle's phase 2 clones with Blue Sight on, and End of Demo. Link them in checkpoint §7.
- [ ] **GitHub Project board:** create it (To do / In progress / Done), add the issues and
  milestones, then put the link, the column counts and a screenshot in checkpoint §5. Send Wajza
  your GitHub usernames so the issues can be assigned. *(everyone)*
- [ ] **(Optional) Profiler capture** of the hub and the arena in the Windows build (CPU ms, GC
  alloc per frame, draw calls) for the performance competency.
- [ ] **Set Company Name** in Project Settings > Player. It is still `DefaultCompany`, which is
  also the save folder's name. Do this before sharing more builds.

### Code: Wajza

- [ ] **A permanent bonus max Smile Star that Star Fragments don't overwrite.**
  After beating Chortle, Pip gets +1 max Smile Star. `PlayerStarFragments` calls
  `PlayerHealth.SetBonusMaxStars(fragments / 4)` on every collect and every load, and that call
  replaces any other bonus.
  **The fix:** make `PlayerHealth` implement `IPermanentStarBonus` (in `Assets/Scripts/Core/SmileStarBonus.cs`):
  ```csharp
  public class PlayerHealth : MonoBehaviour, IDamageable, IPermanentStarBonus
  public void SetPermanentBonusMaxStars(int bonus) // max = start + fragment bonus + permanent bonus
  ```
  Keep the two bonuses in separate fields. `SmileStarBonus` finds the interface and calls it on every level load.
  Until then, a stand-in puts the +1 back after each overwrite. It works, but collecting a
  fragment also refills one star as a side effect.
- [ ] **(Optional) A `DamageType.Coin`.** P3 should only count coin hits, but today a coin hit and a
  mallet hit are both `DamageType.Normal`. Add `Coin` to the enum and pass it in `StarCoin.OnHit`.
  Until then, the balloons accept any hit and are placed out of mallet reach.
- [ ] **(Optional) Static events for sounds.** Add `public static event Action<EnemyBase> AnyPopped;`
  and raise it next to `Popped`, the same way `Pickup.AnyCollected` works. The pop sound then hooks
  up in one line. The same applies to `EnemyBase.Damaged` (coin hits have no sound),
  `ChortleCopy.Popped` and `JackInTheBox.HiddenChanged`.

### Art and audio: Dana

- [ ] Replace the placeholders in section 3 that are still in use. These are wanted next: NPC
  sprites and 56 x 56 dialogue portraits, a dialogue "continue" arrow, journal page art and the
  title screen background.

---

## 2. Keep these working

Other people's code depends on these names. **Don't rename them without telling the team.**

| What | Used by |
|------|---------|
| `ChortleBoss` events `MaskChanged`, `PhaseChanged`, `StateChanged`, `OnDefeated`, plus `StartFight()`, `IsDefeated`, `Mask`/`MaxMask`, `Phase`, `State` | Boss bar (`ChortleHealthSource`), `BossFightFlow`, `BossSounds` |
| Input action names `Swing`, `Throw`, `Dash`, `BlueSight` in the "Player" map | `PlayerAbilities` turns these off until Pip earns them, and while she carries a box in W1. This only applies in scenes with a `LevelInfo` |
| `PlayerCoinThrower.JuggleShotUnlocked` | `PlayerAbilities` |
| `BlueSightOnly`, `BlueSightReactive` (`AlphaWhen` / `SolidWhen`), `BlueSight.SetActive` | W2 secret platform, fake mirrors, `FakeFloor`, tests |
| `PlayerController.Teleport` | W2 mirrors, P2 respawns |
| `PlatformCarrier` moves Pip's `Rigidbody2D.position` with the gondola | W3 Ferris wheel. If `PlayerController` ever gets its own moving-platform code, tell Sarah so the two don't fight |
| Sound events: `Jumped`, `DashingChanged`, `StarLost`, `OnDeath`, `ShieldChanged`, `SwingStarted`, `HitLanded`, `Pogoed`, `CoinsThrown`, `TearBurstReleased`, `GaugeChanged`, `ActiveChanged`, `HazardHit`, `FragmentCollected`, `Pickup.AnyCollected`, `PunchlineBomb.AnyExploded`, `RubberChickenCannon.AnyFired`, Chortle's `AttackWarned` / `AttackLaunched` | `PlayerSounds`, `WorldSounds`, `BossSounds` |
| Layers: Ground = 6, Player = 7, Enemy = 8. The mallet hits Enemy + Default, the coins hit Enemy + Ground | P3 balloons and P4 pots (`BreakableTarget`) are on **Enemy**. A "Breakable" layer doesn't exist |
| The **"Pogo"** tag | The last P4 pot, so a down-swing bounces Pip over the spikes |
| `CINEMACHINE_3` scripting symbol and Cinemachine 3.1.7 (3.1.4 doesn't compile on Unity 6.5) | `ScreenShake` impulses. Tune `globalMultiplier` on each `CameraRig` |
| `BossFightFlow` starts Chortle. Keep **Start Automatically** unticked on him | Intro cutscene → fight |
| `WaveSpawner_Riders` in W3: keep **Start On Player Enter** unticked | `WaveSpawnerBridge` starts it after the 4 switches |

---

## 3. For the artist: placeholders

Placeholders live in `Assets/Art/Placeholders` and `Assets/Audio/Placeholders`. **To replace one,
overwrite the file and keep the same file name.** Unity keeps its ID, so every use updates without any code changes.

| Placeholder | Used for | Notes |
|---|---|---|
| `Fonts/Placeholder_PixelFont.ttf` | All UI text (Press Start 2P, OFL, 8 px) | For a new font: make a TMP font asset (Raster, Point filter), set it as the Default Font Asset in Project Settings > TextMesh Pro, and rebuild the GameSystems prefab |
| `UI/Placeholder_UI_Star.png` | HUD Smile Stars | Tinted full or empty in `HudStars`. A white icon works best |
| `UI/Placeholder_UI_Shield.png`, `_Coin.png`, `_Fragment.png` | HUD icons | |
| `UI/Placeholder_UI_Page.png` | Torn journal pages lying in levels | |
| `UI/Placeholder_UI_Panel.png`, `_Button.png` | Every panel and button | 9-sliced, 2 px border. The button colours are tints |
| `UI/Placeholder_UI_Bar.png` | Tear Gauge, Blue Sight and boss bars | Image Type = Filled |
| Title scene `Logo` text | Title logo | Replace with an Image named `Logo` |
| `Level/Placeholder_Door.png` | The remaining doors (station, task rooms, Laugh Tower) | 16 x 32, pivot at the bottom |
| `Placeholder_sfx_*`, `_jingle_*`, `_ui_*` `.wav` (38 files) | Every sound effect | Generated chip-tune beeps. For more than one version of a sound, add the clips to its entry in `Resources/Audio/SoundLibrary` (one is picked at random) |
| `Placeholder_music_*.wav` (8 files) | Title, Tumbleton, train, Wonderwick, audition, boss, boss final phase, credits | Seamless loops. The audition track must stay at **100 BPM**, or the rhythm game's BPM must be changed to match |
| `Resources/Placeholder_White.png` | Test scenes only | |

**New Pip frames:** keep her shoes on the same pixel row in every frame, because the per-frame pivots
sit at the shoes. Otherwise she bobs.

---

## 4. Building

- **Web (Unity Play):** build profile `Assets/Settings/Build Profiles/Web.asset`. The live build is linked at the top of this file.
- **Windows:** Tools > Clown Who Cried > Build > Windows (64-bit) → `Builds/Windows/TheClownWhoCried.exe`
  (`Builds/` is in .gitignore). Only the title screen and the 10 levels are included.
- **Mac:** add **Mac Build Support (Mono)** to Unity 6000.5.9f1 in Unity Hub, then Tools > Clown Who Cried > Build > Mac.
- **Smoke test:** `TheClownWhoCried.exe -batchmode -nographics -smoketest -logFile smoketest.log`.
  The log should end with `SMOKETEST PASS`.
- A build makes Unity rewrite some URP and Project Settings files (shader stripping caches).
  Don't commit those changes unless you changed a setting on purpose.

---

## 5. Demo polish (commit `798f003`): read this before changing scenes

This is the **base for Phase 3**. Everything is already in the scenes and prefabs.

- **The setup tools are hidden** behind `#if CWC_SETUP_TOOLS`. **Don't re-run them**, because they
  rebuild scenes and undo manual changes. If you really need one, add the symbol, use the tool, then remove the symbol.
- **Camera** (`Assets/Scripts/Polish`): `PipCameraFollow` runs in every level at zoom 7, and the
  Pixel Perfect Camera is turned off on load. `AspectLock` keeps 16:9. `FixedCameraRoom` is used
  for the bedroom, the doctor's office and the Laugh Tower. `KeepCameraInBounds` is used for
  Tumbleton, the hub and W1–W4.
- **Rooms:** Pip's house, the doctor's office and the Laugh Tower have no doors. Pip walks out
  the right side (`LevelExit`). The interactables are the painted mirror and Dr. Mellow himself.
- **Pip:** `PipAnimator` syncs the mallet to `SwingPhase` and uses air poses. Her feet stay
  planted while throwing, and she uses a platform-aware grounded check. `dashStretch` = (1.12, 1).
- **Enemies:** Pip and enemies pass through each other (`EnemyPassThrough`), and contact damage
  uses overlaps. The Wobbler pounces at Pip and bounces off her. The Pie Slinger and Chortle use
  `Pie_Left` or `Pie_Right` depending on which side Pip is on.
- **Boss arena:** there are no trapdoors and the cannons are hidden. Chortle spawns in the middle
  and teleports anywhere. His copies are the same size (0.65) and spread out. A "!" bubble warns
  before each chicken. With Blue Sight on, the fakes fade to 0.25 opacity.
- **Blue Sight look:** the world is washed out (`WashedOutView`). Blue Sight shows full colour with no blue tint.

---

## 6. Done

- [x] The "Pogo" tag added to `TagManager` (Part 5).
- [x] Real prefabs committed in `Assets/Prefabs/{Player, Combat, Pickups, Enemies, Boss, Effects}`.
  `GameSettings > Player Prefab` = `Prefabs/Player/Pip`. `Placeholder_Pip` and the placeholder `Coin` were deleted.
- [x] Pip's coins and mallet hit the Enemy layer.
- [x] W3 "runaway riders": 3 waves (Wobblers, then a Pie Slinger joins).
- [x] The real Chortle is in the Laugh Tower with `ChortleFight` + `ChortleHealthSource`. `PlaceholderBoss`, `TrapdoorFloor` and `TrapdoorSection` were deleted.
- [x] Placeholder tiles replaced by the real tiles in `Assets/Art/Tiles/Tile Assets`.
- [x] `studentB-core` merged into `main` (PR #3, Nov 16).
- [x] Internal playtest written up (`docs/phase2/playtest_internal_wajza.md`).
- [x] Web build published on Unity Play. `README.md` added.
