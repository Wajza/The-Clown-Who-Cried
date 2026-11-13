/// <summary>
/// Anything that can be stunned (stops acting for a while), e.g. by Pip's Tear Burst.
/// Enemies implement this next to IDamageable. A boss can implement it and ignore or shorten stuns.
/// </summary>
public interface IStunnable
{
    void Stun(float duration);
}
