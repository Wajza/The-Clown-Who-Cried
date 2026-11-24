# Phase 2 Playtest: Internal Session (Wajza)

This is an **internal** playtest by a team member, so it isn't a blind test. The sessions focused on finding bugs and polish problems while the demo was being finished. Everything below was seen in play. Times and death counts weren't recorded during these sessions. 

## Session

| | |
|---|---|
| Player | Wajd Alghamdi (team member, developer) |
| Has played platformers before? | yes |
| Dates | 2026-11-18 → 2026-11-24 (several sessions) |
| Build / commit | Unity Editor, `main` before the polish commit `798f003`. Fixes were re-checked on later builds |
| Input | keyboard and mouse |
| Facilitator | None (self-test) |
| Type | Bug-hunting and polish pass, not timed |

## Findings by segment

| Segment | What was seen | Severity | Fixed in `798f003` |
|---------|---------------|----------|--------------------|
| **Everywhere** | The camera didn't stay on Pip, and showed the area outside the background | P0 | ✅ `PipCameraFollow`, confiners |
| **Everywhere** | Screen size wasn't fixed, and the characters were different sizes in different scenes | P1 | ✅ 16:9 lock, zoom 7, sprite scale and PPU fixes |
| Pip's house (P1) | The background didn't fit the room. The mirror to use was an extra object instead of the painted one in the background | P1 | ✅ Room fitted to the background. The painted mirror is the interactable |
| Pip's house (P1) | Using a door to leave the bedroom felt unnecessary | P2 | ✅ Walk out the right side |
| Doctor's office (P5) | Extra props were added on top of things already painted in the background | P2 | ✅ Removed |
| Doctor's office (P5) | The interaction was attached to empty space instead of to Dr. Mellow | P1 | ✅ The doctor is the interactable |
| Tumbleton (P2–P4) | Throwing: Pip's feet and body moved even while she stood still | P1 | ✅ Feet-planted pivots for each frame |
| Tumbleton (P2–P4) | Dashing: Pip looked like she was "dashing in another lane" (she dropped lower) | P1 | ✅ `dashStretch` changed to (1.12, 1) |
| Tumbleton (P4) | The mallet animation didn't match the hit | P1 | ✅ `SwingPhase` sync |
| Wonderwick hub | The camera showed outside the background | P1 | ✅ `KeepCameraInBounds` |
| W1–W4 | The camera showed outside the level | P1 | ✅ `KeepCameraInBounds` |
| W1–W4 | Riding a moving platform, Pip looked like she was falling | P1 | ✅ Platform-aware grounded check |
| Enemies: Whoopee Wobbler | It went under the floor, got bigger when attacking, and didn't bounce on Pip | P0 | ✅ Pass-through, alert sprite PPU, pounce and bounce. **Under-floor fix still to be confirmed in play** |
| Enemies: Pie Slinger | Pies always flew left, even when Pip was on the right | P1 | ✅ Left/right pie prefabs |
| Chortle arena | Trapdoors weren't wanted. The platforms were good | P2 | ✅ Trapdoors removed |
| Chortle arena | Chortle didn't start in the middle or use the platforms | P1 | ✅ Spawns in the middle, teleports anywhere |
| Chortle arena (phase 2) | The clones were smaller than Chortle and bunched in one place | P1 | ✅ Same size, scattered |
| Chortle arena | The cannons cluttered the arena, and the chickens came without warning | P1 | ✅ Cannons hidden, "!" warning before each chicken |
| Chortle arena (phase 2) | Blue Sight didn't help pick out the real Chortle | P1 | ✅ Fakes go see-through with Blue Sight |
| Blue Sight (W2, boss) | The blue tint hid the world's real colours | P2 | ✅ Blue Sight shows normal full colour |

**Totals:** 20 findings: 3 P0, 13 P1, 4 P2. All 20 are fixed, and 1 still needs confirming in play.

## After playing (tester rates 1–5)

| Question | 1–5 | Comment |
|----------|-----|---------|
| The controls felt responsive | yes | |
| I always knew what to do next | yes | |
| I could tell when an enemy was about to attack | yes | |
| I could find the real Chortle with Blue Sight | yes | |
| The camera showed what I needed | yes | |
| The difficulty felt fair | yes | |

## Limits of this session

- The tester knows the game, so it doesn't test whether a new player understands what to do. That is what the outside playtest is for (see `playtest_form.md`).
- It wasn't timed. Use the outside sessions to measure time for each segment and the number of deaths.
- Fixes were checked by the developer only. **Next:** re-run the 11 automated test scenes, then confirm the Wobbler fix in play.
