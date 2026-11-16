using UnityEngine;

/// <summary>
/// Connects my teammate's ChortleBoss to the boss health bar. Put it on the same GameObject as
/// ChortleBoss (or anywhere in the arena, with the boss assigned).
///
/// Listens to ChortleBoss.MaskChanged(mask, max), PhaseChanged(phase), StateChanged(state) and
/// OnDefeated. The "health" shown is Chortle's Mask. The fight counts as on once Chortle leaves
/// the Waiting state, and over when he's Defeated.
/// </summary>
public class ChortleHealthSource : BossHealthSource
{
    [SerializeField, Tooltip("The boss. Empty = the ChortleBoss on this GameObject.")]
    private ChortleBoss boss;

    [SerializeField] private string displayName = "Grand Jester Chortle";

    public override string BossName => displayName;
    public override float Health => boss != null ? boss.Mask : 0f;
    public override float MaxHealth => boss != null ? boss.MaxMask : 1f;
    public override int Phase => boss != null ? boss.Phase : 0;
    public override bool IsFighting =>
        boss != null && boss.State != ChortleState.Waiting && boss.State != ChortleState.Defeated;

    private void Awake()
    {
        if (boss == null)
            boss = GetComponent<ChortleBoss>();
    }

    protected override void OnEnable()
    {
        if (boss != null)
        {
            boss.MaskChanged += OnMaskChanged;
            boss.PhaseChanged += OnPhaseChanged;
            boss.StateChanged += OnStateChanged;
            boss.OnDefeated += RaiseChanged;
        }
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        if (boss != null)
        {
            boss.MaskChanged -= OnMaskChanged;
            boss.PhaseChanged -= OnPhaseChanged;
            boss.StateChanged -= OnStateChanged;
            boss.OnDefeated -= RaiseChanged;
        }
        base.OnDisable();
    }

    private void OnMaskChanged(float mask, float max) => RaiseChanged();
    private void OnPhaseChanged(int phase) => RaiseChanged();
    private void OnStateChanged(ChortleState state) => RaiseChanged();
}
