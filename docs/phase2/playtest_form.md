# Phase 2 Playtest Form: The Clown Who Cried

Use one copy for each player. The facilitator fills it in while watching. **Don't help the player** unless they are stuck for more than 2 minutes, and write down every time you do.

## Session

| | |
|---|---|
| Player (name or ID) | Raneem|
| Has played platformers before? (none / some / a lot) | some |
| Date |24 - 11 - 2026 |
| Build / commit | polished demo chapter 1 (v1.0) |
| Input | keyboard and mouse |
| Facilitator ||

## Script

1. Start from the **Title** screen with **New Game**. Say only: *"Play until the game ends. Think out loud."*
2. Time each segment, and count deaths and the times you had to help.

| Segment | Time (mm:ss) | Deaths | Helped? (what) | Notes: confusion, bugs, fun moments |
|---------|--------------|--------|----------------|-------------------------------------|
| Opening and Pip's house (P1) | 02:40 | 0 | No | Read all the dialogue. Walked around for ~30 s before noticing the painted mirror was interactable. Said "oh, it's part of the wall". |
| P2 Bumper-car street (Dash) | 03:15 | 2 | No | Both deaths on the second bumper car: dashed too early. Laughed at the first dash: "that's so fast!" |
| P3 Balloons (Star Coins) | 02:05 | 1 | No | Fell once going for a high Star Coin. Asked out loud "do the coins do anything?" |
| P4 Pots and pogo (Mallet) | 04:30 | 3 | Yes: told them they can pogo by hitting **down** in the air (stuck for 2+ min) | Didn't find the pogo alone. After the hint, chained 4 pogos and cheered. |
| P5 Doctor's office (Tear Burst) | 01:50 | 0 | No | Understood Tear Burst straight away. Said the scene with Dr. Mellow was "kind of sad, in a good way". |
| Train map → Wonderwick hub | 01:10 | 0 | No | Clicked the wrong station first, then found Wonderwick. Liked the hub music. |
| W1 Punchline Delivery (timer) | 03:45 | 2 | No | Both deaths from the timer running out. Didn't notice the timer until the first fail: "wait, there was a timer?" |
| W2 Mirror Maze (Blue Sight) | 04:10 | 1 | No | Turned on Blue Sight by accident, then used it on purpose. Got briefly lost near the middle of the maze. |
| W3 Coaster (wheel, switches, waves) | 05:20 | 4 | No | Hardest segment. 3 deaths on the waves after the second switch. Unsure which switch she had already hit. |
| W4 Audition (rhythm) | 02:30 | 1 | No | Said "this is the most fun part". Missed the beat once on the fast section. |
| Chortle phase 1 | 01:40 | 1 | No | Got hit by the first chicken, then started watching for the "!" warning. |
| Chortle phase 2 (clones) | 02:55 | 2 | No | Hit clones twice before remembering Blue Sight. After that, found the real Chortle every time. |
| Chortle phase 3 (bombs) | 02:20 | 1 | No | Bombs were readable. One death from standing on a platform edge. |
| End of Demo reached? (yes/no) | **yes** (total ≈ 38:10) | **18** | **1** | Wobbler went partly into the floor once in Tumbleton (P2). |

## After playing (ask the player, 1–5)

| Question | 1–5 | Comment |
|----------|-----|---------|
| The controls felt responsive | 4 | "Dash feels great. Jumping off the coaster felt a bit floaty." |
| I always knew what to do next | 3 | Missed the pogo and the W1 timer. |
| I could tell when an enemy was about to attack | 4 | The "!" warning on chickens helped. The Pie Slinger was harder to read. |
| I could find the real Chortle with Blue Sight | 4 | Easy once remembered. Forgot it existed at the start of phase 2. |
| The camera showed what I needed | 5 | No complaints. |
| The difficulty felt fair | 3 | W3 felt like a big jump in difficulty compared to the rest. |

- **What was the most confusing moment?** "The pots level. I didn't know I could bounce on things with the mallet."
- **What was the most fun moment?** "The audition, and pogoing once I figured it out."
- **Did anything look broken?** "A balloon enemy sank into the ground for a second on the bumper-car street."

## Triage (the team fills this in afterwards)

| # | Finding | Severity (P0 blocks / P1 wrong / P2 polish) | Owner | Planned fix |
|---|---------|---------------------------------------------|-------|-------------|
| 1 | Player didn't discover the mallet pogo without help (P4) | P1 | Design | Add a short pogo tutorial prompt over the first pot gap |
| 2 | W1 timer wasn't noticed until the first fail | P1 | UI | Make the timer bigger, and flash it plus play a tick sound for the last 10 s |
| 3 | W3 waves section has a difficulty spike (4 deaths) | P1 | Design | Add a checkpoint after the second switch. Show switch state (lit/unlit) |
| 4 | Whoopee Wobbler sank into the floor once (P2) | P1 | Programming | Re-check the under-floor fix from `798f003` and add a test scene for it |
| 5 | Player forgot Blue Sight at the start of Chortle phase 2 | P2 | Design | Have Chortle say a hint line ("Can you see the real me?") when clones appear |
| 6 | Train map: clicked the wrong station first | P2 | UI | Highlight the next station on the map |
| 7 | Star Coins: player unsure what they're for | P2 | Design / UI | Show the coin count and its use on the hub screen |
