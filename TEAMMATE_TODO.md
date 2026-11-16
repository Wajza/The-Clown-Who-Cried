# TEAMMATE_TODO

Things my teammate (player, combat, enemies, boss) or the artist need to do so that my systems
(core, dialogue, tasks, UI, levels, cutscenes, audio) fully connect. My code never edits their
files; where something is missing, it uses a TODO plus an event or interface and works once
they add it. Newest items are at the bottom of each list.

Status: [ ] open, [x] done

---

## For my teammate (code and prefabs)

- [x] **1. The "Pogo" tag.** Done by me in Part 5: it's now in `ProjectSettings/TagManager.asset`
  after Spikes, Pit and Boss. Nothing to do; just don't remove it when merging. The P4 "Reflex
  Check" pot over the spikes uses it, and `PlayerMallet` already pogos off it.

- [ ] **2. A permanent bonus max Smile Star that Star Fragments don't overwrite.**
  - After beating Chortle, Pip gets +1 max Smile Star (and journal page 2).
  - Problem: `PlayerStarFragments` calls `PlayerHealth.SetBonusMaxStars(fragments / 4)` on every
    collect and every load, which replaces any other bonus.
  - **Exactly what to add (Part 8 is ready for it):** make `PlayerHealth` implement my interface
    `IPermanentStarBonus` (in `Assets/Scripts/Core/SmileStarBonus.cs`):
    `public class PlayerHealth : MonoBehaviour, IDamageable, IPermanentStarBonus` and
    `public void SetPermanentBonusMaxStars(int bonus)`, so that
    max = start + fragment bonus + permanent bonus (keep both bonuses in separate fields).
    My `SmileStarBonus` finds it automatically and calls it on every level load.
  - Until then a **stand-in** re-applies the +1 every time `PlayerStarFragments` overwrites it.
    It works (tested), but collecting a fragment refills one star as a side effect.

- [ ] **3. (Optional) A "Coin" damage type.**
  - P3 "Pop the Balloon Wall" should only count coin hits. Today `DamageInfo` can't tell a coin
    from a mallet hit (both are `DamageType.Normal`, both from Pip).
  - Suggestion: add `DamageType.Coin` and pass it in `StarCoin.OnHit`. Until then my balloons
    accept any hit and are placed out of mallet reach.

- [ ] **4. Commit your prefabs.** No branch has any prefab from you yet, only scripts. My levels
  (Part 5) and the boss fight (Part 8) need, with every Inspector field filled in:
  - **Pip** (all player components, pivot at her feet, Player layer, input actions assigned).
  - **Star Coin** (for `PlayerCoinThrower.coinPrefab`).
  - Each **enemy** you want in the levels (Jack-in-the-box, pie thrower...).
  - **Grand Jester Chortle** and his arena pieces (trapdoors, cannons...).
  - Please commit each `.meta` with its prefab, so the IDs match on every computer.
  - **Using the real Pip (one step):** every level spawns the Pip set in
    `Assets/Resources/GameSettings` > **Player Prefab**. It's `Placeholder_Pip` (a pink box) for now.
    Drag your Pip prefab into that field and every level uses it. Her pivot must be at her feet.

- [ ] **5. Boss health bar hookup (nothing to code).** My `ChortleHealthSource` listens to
  `ChortleBoss.MaskChanged / PhaseChanged / StateChanged / OnDefeated`. Please don't rename those
  events, or tell me if you do.

- [ ] **6. Your ScreenShake now uses Cinemachine (please check it feels right).** Part 5 installed
  **Cinemachine 3.1.7** (3.1.4 doesn't compile on Unity 6.5) and added the **`CINEMACHINE_3`**
  scripting symbol (Project Settings > Player > Scripting Define Symbols), so the Cinemachine code
  inside `ScreenShake` is switched on. Every level's `CameraRig` has your `ScreenShake` + a
  `CinemachineImpulseSource`, and the camera has a `CinemachineImpulseListener`. Tune
  `globalMultiplier` on the rig if the shake is too strong or weak.

- [ ] **7. Abilities are switched off by input action names.** `PlayerAbilities` (my code) turns
  off single actions in your "Player" map until Pip earns them: `Swing` (mallet, P4), `BlueSight`
  (W2), and `Swing`/`Throw`/`Dash` while carrying a box (W1). Juggle Shot uses
  `PlayerCoinThrower.JuggleShotUnlocked`. Please keep those action names, or tell me if they change.
  This only happens in my levels (scenes with a `LevelInfo`); your test scenes are untouched.

- [ ] **8. Pip's coins and mallet must hit the Enemy layer.** P3's balloons and P4's pots
  (`BreakableTarget`, my script, implements your `IDamageable`) are on the **Enemy** layer,
  because it's the only target layer the project has (the tooltips mention a "Breakable" layer,
  but it doesn't exist). On your Pip prefab, please include **Enemy** in
  `PlayerCoinThrower > Hit Layers` and `PlayerMallet > Hit Layers`. The targets are triggers;
  your mallet uses `useTriggers = true` already, and coins use a CircleCast (hits triggers when
  Physics 2D "Queries Hit Triggers" is on, the default).
  - The last P4 pot is tagged **"Pogo"**, so a down-swing on it bounces Pip over the spikes.
  - Until your Pip prefab exists, P3/P4 can't be played by hand: `Placeholder_Pip` has no coin
    thrower (no Star Coin prefab) and no mallet set-up. My automatic test hits the targets directly.

- [ ] **9. Fill in the W3 "runaway riders" waves.** In `Wonderwick_W3_Coaster`, the object
  `WaveSpawner_Riders` has your `WaveSpawner` with 2 spawn points and **no waves yet**. Please add
  the waves (your enemy prefabs). My `WaveSpawnerBridge` (same object) starts it after Pip flips
  the 4 switches and finishes the task on `OnAllWavesCleared`. Keep **Start On Player Enter**
  unticked. With no waves the spawner counts as cleared at once, so the task works already.

- [ ] **10. Things I built on top of your code (please keep these working):**
  - W2 uses your **`BlueSightOnly`** (the secret platform, the cracks on fake mirrors) and my
    `FakeFloor`, which inherits your **`BlueSightReactive`** (`AlphaWhen` / `SolidWhen`). The test
    also uses `BlueSight.SetActive`.
  - W3's Ferris wheel gondolas use my `PlatformCarrier`: while Pip stands on one, it moves her
    `Rigidbody2D.position` by as much as the gondola moved each physics step. If your
    `PlayerController` ever gets its own moving-platform code, tell me so they don't fight.
  - W2's real mirrors and P2's respawns use your `PlayerController.Teleport`.

- [ ] **11. Put the real Chortle in the Laugh Tower.** `ChortleArena` has a
  `PlaceholderBoss (replace with Chortle)` (a purple box that takes hits) so the whole boss flow
  works now. When your Chortle prefab exists:
  1. Drop it in `ChortleArena`, and **untick "Start Automatically"** (my `BossFightFlow` starts
     him with `StartFight()` after the intro cutscene).
  2. Add my `ChortleFight` and `ChortleHealthSource` components to him.
  3. In `Boss (Part 8) > BossFightFlow`, drag him into **Boss**, then delete the PlaceholderBoss.
  My code uses `ChortleBoss.StartFight()`, `IsDefeated`, `OnDefeated`, `Mask/MaxMask`, `Phase`,
  `State` and the events listed in #5.

- [ ] **12. (Optional) A static "any enemy popped" event, for the pop sound.** My audio listens
  to your events for sounds (`PlayerSounds`, `WorldSounds`, `BossSounds`; nothing in your code
  changes). Enemies only have a per-enemy `EnemyBase.Popped`, so there's no pop sound yet. If you
  add `public static event Action<EnemyBase> AnyPopped;` (raised next to `Popped`), like your
  `Pickup.AnyCollected`, I'll hook up the sound in one line. The final review (Part 10) found the
  same for `EnemyBase.Damaged` (a coin hitting an enemy has no sound; mallet hits do, via
  `HitLanded`), `ChortleCopy.Popped` and `JackInTheBox.HiddenChanged`: static versions of those
  would let me add their sounds too.
  The sounds already connected: Jumped, DashingChanged, StarLost, OnDeath, ShieldChanged,
  SwingStarted, HitLanded, Pogoed, CoinsThrown, TearBurstReleased, GaugeChanged, ActiveChanged
  (Blue Sight), HazardHit, FragmentCollected, Pickup.AnyCollected, PunchlineBomb.AnyExploded,
  RubberChickenCannon.AnyFired, and Chortle's AttackWarned / AttackLaunched / PhaseChanged / OnDefeated.
  Please keep those names (or tell me).

## For the artist

Every placeholder lives in `Assets/Art/Placeholders`. **To replace one, overwrite the file and
keep the same file name.** Unity then keeps its ID, and every use updates without code changes.

| Placeholder | Used for | Notes |
|---|---|---|
| `Fonts/Placeholder_PixelFont.ttf` | All UI text (Press Start 2P, free OFL font, 8 px) | For a different font: make a TMP font asset (Raster, Point filter) and set it as the Default Font Asset in Project Settings > TextMesh Pro, then rebuild the GameSystems prefab |
| `UI/Placeholder_UI_Star.png` | HUD Smile Stars | Full/empty are tinted in `HudStars`; a white icon works best |
| `UI/Placeholder_UI_Shield.png` | HUD shield icon | |
| `UI/Placeholder_UI_Coin.png` | HUD Star Coins | |
| `UI/Placeholder_UI_Fragment.png` | HUD Star Fragment counter | |
| `UI/Placeholder_UI_Page.png` | Torn journal page lying in levels | |
| `UI/Placeholder_UI_Panel.png` | Every panel/box (dialogue, journal, menus) | 9-sliced, 2 px border (Sprite Editor) |
| `UI/Placeholder_UI_Button.png` | Every menu button | 9-sliced, 2 px border. Normal/selected colours are tints on the Button |
| `UI/Placeholder_UI_Bar.png` | Tear Gauge, Blue Sight and boss bars | Image Type = Filled |
| Title scene `Logo` text | Title screen logo | Replace with an Image named `Logo` |
| `Tiles/Placeholder_Tile_GroundTop.png` | Top row of every level's floor (grass) | 16 x 16. The `.asset` next to it is the Tile used in the Tile Palette |
| `Tiles/Placeholder_Tile_Ground.png` | Floor below the top row (earth) | 16 x 16 |
| `Tiles/Placeholder_Tile_Wall.png` | Level end walls (bricks) | 16 x 16 |
| `Level/Placeholder_Door.png` | Every door (houses, station, task rooms, Laugh Tower) | 16 x 32, pivot at the bottom |
| `Assets/Prefabs/Placeholders/Placeholder_Pip.prefab` | Pip in every level until the real prefab exists | See teammate item 4 |
| `Assets/Audio/Placeholders/Placeholder_sfx_*.wav` (37 files) | Every sound effect: `sfx_jump`, `sfx_hit`, `jingle_task_done`... | Generated chip-tune beeps. Overwrite with the real sound, same name. More than one version of a sound? Add the extra clips to its entry in `Assets/Resources/Audio/SoundLibrary` (one is picked at random) |
| `Assets/Audio/Placeholders/Placeholder_music_*.wav` (8 files) | Music: title, Tumbleton, train, Wonderwick, audition (100 BPM), boss, boss final phase, end credits | Loops seamlessly. The audition track should stay at 100 BPM (the rhythm game's speed) or change the rhythm game's BPM to match. Volumes per sound are in the SoundLibrary |
| `Assets/Resources/Placeholder_White.png` | Test-scene squares (floor, NPCs) | Test scenes only |

Wanted later: NPC sprites and portraits (56 x 56 for the dialogue box), a dialogue "continue"
arrow, journal page art (`JournalPageData.picture`), and the title screen background.

## Building the game

- **Windows:** Tools > Clown Who Cried > Build > Windows (64-bit) -> `Builds/Windows/TheClownWhoCried.exe`
  (about 104 MB; `Builds/` is in .gitignore). Only the title screen and the 10 levels go in; test
  scenes, test-only tasks/pages and `SampleScene` stay out.
- **Mac:** needs **Mac Build Support (Mono)** added to Unity 6000.5.9f1 in Unity Hub
  (Installs > gear > Add modules), then Tools > Clown Who Cried > Build > Mac.
- **Check a build:** run `TheClownWhoCried.exe -batchmode -nographics -smoketest -logFile smoketest.log`;
  the log ends with `SMOKETEST PASS` (it plays title -> New Game -> Wonderwick -> boss -> End of Demo).
- Building makes Unity rewrite a few URP/Project Settings files (shader stripping caches). That's
  normal; don't commit those changes unless you changed a setting on purpose.
- Before sharing a build: set **Company Name** in Project Settings > Player (it's still
  "DefaultCompany", which is also the save folder's name).

## For the whole team

- [ ] **Merge `studentB-core` into `main`.** `main` still has the wrong `.meta` IDs for the
  player/combat scripts (from an old merge), so scenes and prefabs made on another computer
  show "Missing Script" there. `studentB-core` has the fixed IDs:
  https://github.com/Wajza/The-Clown-Who-Cried/pull/new/studentB-core
