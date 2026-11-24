using UnityEngine;

/// <summary>
/// Pip and enemies pass through each other instead of pushing each other around (as EnemyBase's
/// contact damage was designed for: it checks overlaps, so touching an enemy still hurts Pip).
///
/// Why: with Player and Enemy colliding, Pip landing on or pushing into a Whoopee Wobbler squeezed it
/// between her and the floor, and it got pushed through the seams between floor tiles: it ended up
/// under the floor. Enemies still stand on the ground and walls as before.
///
/// Runs by itself when the game starts (no setup).
/// </summary>
public static class EnemyPassThrough
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        int player = LayerMask.NameToLayer("Player");
        int enemy = LayerMask.NameToLayer("Enemy");
        if (player >= 0 && enemy >= 0)
            Physics2D.IgnoreLayerCollision(player, enemy, true);
    }
}
