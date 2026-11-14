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
  - Suggestion: in `PlayerHealth`, add something like
    `public void SetPermanentBonusMaxStars(int bonus)` and count it next to the fragment bonus
    (max = start + fragmentBonus + permanentBonus). My save system will call it on load and
    after Chortle (Part 8). Tell me the final method name.

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
| `Assets/Resources/Placeholder_White.png` | Test-scene squares (floor, NPCs) | Test scenes only |

Wanted later: NPC sprites and portraits (56 x 56 for the dialogue box), a dialogue "continue"
arrow, journal page art (`JournalPageData.picture`), and the title screen background.

## For the whole team

- [ ] **Merge `studentB-core` into `main`.** `main` still has the wrong `.meta` IDs for the
  player/combat scripts (from an old merge), so scenes and prefabs made on another computer
  show "Missing Script" there. `studentB-core` has the fixed IDs:
  https://github.com/Wajza/The-Clown-Who-Cried/pull/new/studentB-core
