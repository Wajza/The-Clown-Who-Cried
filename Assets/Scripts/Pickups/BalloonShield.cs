/// <summary>Gives Pip a shield that absorbs the next hit. Stays in the world if Pip already has one.</summary>
public class BalloonShield : Pickup
{
    protected override bool TryApply(PlayerController player)
    {
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        return health != null && health.GiveShield(); // GiveShield returns false if already shielded.
    }
}
