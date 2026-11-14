# TEAMMATE_TODO

Things my teammate (player, combat, enemies, boss) or the artist need to do so that my systems
(core, dialogue, tasks, UI, levels, cutscenes, audio) fully connect. My code never edits their
files; where something is missing, it uses a TODO plus an event or interface and works once
they add it. Newest items are at the bottom of each list.

Status: [ ] open, [x] done

---

## For my teammate (code and prefabs)

- [ ] **1. Add the "Pogo" tag** (Project Settings > Tags and Layers > Tags).
  - The P4 "Reflex Check" task needs it: the last pot hangs over spikes and is tagged "Pogo",
    so a down-swing bounces Pip over. `PlayerMallet` already pogos off anything tagged "Pogo"
    (its `pogoTags`); the tag just doesn't exist yet (only Spikes, Pit and Boss do).
  - `ProjectSettings/TagManager.asset` is shared. If I add it first (planned for Part 5), I'll say so here.

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

- [ ] **5. Boss health bar hookup (nothing to code).** My `ChortleHealthSource` listens to
  `ChortleBoss.MaskChanged / PhaseChanged / StateChanged / OnDefeated`. Please don't rename those
  events, or tell me if you do.

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
| `Assets/Resources/Placeholder_White.png` | Test-scene squares (floor, NPCs) | Test scenes only |

Wanted later: NPC sprites and portraits (56 x 56 for the dialogue box), a dialogue "continue"
arrow, journal page art (`JournalPageData.picture`), and the title screen background.

## For the whole team

- [ ] **Merge `studentB-core` into `main`.** `main` still has the wrong `.meta` IDs for the
  player/combat scripts (from an old merge), so scenes and prefabs made on another computer
  show "Missing Script" there. `studentB-core` has the fixed IDs:
  https://github.com/Wajza/The-Clown-Who-Cried/pull/new/studentB-core
