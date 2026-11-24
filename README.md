# The Clown Who Cried

A 2D platformer made in Unity. Pip is a young clown in a world that has lost its colour. You travel from Tumbleton to Wonderwick, earn a new ability with each task, and face Chortle in the Laugh Tower.

**▶ Play it in your browser:** [The Clown Who Cried on Unity Play](https://play.unity.com/en/games/e31d1144-ad83-414e-82d4-0e443815c874/the-clown-who-cried)

SWE 402 team project · Phase 2: systems vertical slice (demo chapter 1)

---

## The demo

The demo runs from the **Title** screen to the **End of Demo** screen in one go:

1. **Tumbleton prologue.** Leave Pip's house, then complete the town tasks. Each one unlocks an ability: **Dash**, **Star Coins**, the **Mallet** and **Tear Burst**.
2. **Train map.** Take the train to Wonderwick.
3. **Wonderwick.** Finish four tasks from the hub: a timed delivery, a mirror maze solved with **Blue Sight**, a Ferris wheel and coaster challenge, and a rhythm audition.
4. **Boss: Chortle.** The fight has three phases: pie volleys, then clones you find with Blue Sight, then bombs that only Tear Burst can stop.

Progress saves automatically. **Continue** on the Title screen picks up where you left off.

## Controls

| Action | Keyboard and mouse | Gamepad |
|--------|--------------------|---------|
| Move | WASD / arrow keys | Left stick |
| Jump | Space | A / South |
| Dash | Left Shift | Right bumper |
| Mallet swing (hold down in the air to pogo) | K / right mouse | X / West |
| Throw Star Coin | J / left mouse | Right trigger |
| Tear Burst | E | Y / North |
| Blue Sight | Q | Left trigger |
| Interact / talk | F | B / East |
| Advance dialogue | Space / Enter / F / left mouse | A / South |
| Journal | Tab | Select |
| Pause | Esc | Start |

Abilities stay locked until the story gives them to you. The in-game **Controls** screen (under Pause) always shows the current bindings.

## Running the project

- **Unity 6000.5.9f1** with URP 2D.
- Clone the repository and open the folder in Unity Hub with that editor version.
- Open the `Title` scene and press Play. The scene order is set in **Build Settings**.
- **F12** saves a screenshot in Play Mode (editor only).

### Tests

The 11 automated test scenes in `Assets/Scenes/Tests` play through the real systems. They write PASS/FAIL lines to `Logs/*TestResults.txt`.

## Project layout

| Folder | What it holds |
|--------|---------------|
| `Assets/Scripts` | Game code: player, combat, enemies, boss, core, dialogue, tasks, UI, polish, tests |
| `Assets/Scenes` | Game scenes (Title, Tumbleton, Wonderwick, ChortleArena, EndOfDemo…) and `Tests/` |
| `Assets/Prefabs` | Player, combat, pickups, enemies, boss, effects |
| `Assets/Art` | Character animations, environments and placeholders |
| `Assets/Audio` | Placeholder music and sound effects |
| `docs/phase2` | Phase 2 checkpoint, playtest form and playtest reports |

## Known limitations

- The audio is placeholder only.
- The difficulty setting exists in code but has no menu option yet.
- The world doesn't regain its colour yet, because the demo has only one boss.

See [docs/phase2/checkpoint.md](docs/phase2/checkpoint.md) for the full status.

## Team

| Member | Area |
|--------|------|
| Wajza | Player, combat, enemies, boss, game feel, camera, integration and polish |
| Sarah Almas | Core systems: save, scenes, dialogue, tasks, UI, levels, cutscenes, audio, build and tests |
| Dana Alyahya | Character art and animation, environment art |
