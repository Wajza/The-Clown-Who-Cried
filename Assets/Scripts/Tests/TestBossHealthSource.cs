/// <summary>
/// A pretend boss for UISystemTest, so the boss health bar can be checked without the real
/// Chortle (who needs his whole arena). The test calls StartFight, Damage, SetPhase and Defeat.
/// </summary>
public class TestBossHealthSource : BossHealthSource
{
    private string _name = "Test Boss";
    private float _health, _max = 1f;
    private int _phase;
    private bool _fighting;

    public override string BossName => _name;
    public override float Health => _health;
    public override float MaxHealth => _max;
    public override int Phase => _phase;
    public override bool IsFighting => _fighting;

    public void StartFight(string bossName, float maxHealth)
    {
        _name = bossName;
        _health = _max = maxHealth;
        _phase = 1;
        _fighting = true;
        RaiseChanged();
    }

    public void Damage(float amount)
    {
        _health = UnityEngine.Mathf.Max(0f, _health - amount);
        RaiseChanged();
    }

    public void SetPhase(int phase)
    {
        _phase = phase;
        RaiseChanged();
    }

    public void Defeat()
    {
        _fighting = false;
        RaiseChanged();
    }
}
