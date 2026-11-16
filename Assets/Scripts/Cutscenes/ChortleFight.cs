using System;
using UnityEngine;

/// <summary>
/// Connects my teammate's Grand Jester Chortle (ChortleBoss) to BossFightFlow.
/// StartFight calls ChortleBoss.StartFight(); Defeated fires on ChortleBoss.OnDefeated.
///
/// Setup (once my teammate's Chortle prefab exists, TEAMMATE_TODO #4):
///   1. Put Chortle in ChortleArena and UNTICK his "Start Automatically" (BossFightFlow starts
///      him after the intro cutscene).
///   2. Add this script and ChortleHealthSource (boss bar) to him.
///   3. In BossFightFlow, drag him into "Boss" instead of the PlaceholderBoss, and delete that.
/// </summary>
public class ChortleFight : MonoBehaviour, IBossFight
{
    [SerializeField, Tooltip("Empty = the ChortleBoss on this GameObject.")]
    private ChortleBoss boss;

    public event Action Defeated;

    public bool IsDefeated => boss != null && boss.IsDefeated;

    public void StartFight()
    {
        if (boss != null)
            boss.StartFight();
    }

    private void Awake()
    {
        if (boss == null)
            boss = GetComponent<ChortleBoss>();
    }

    private void OnEnable()
    {
        if (boss != null)
            boss.OnDefeated += OnBossDefeated;
    }

    private void OnDisable()
    {
        if (boss != null)
            boss.OnDefeated -= OnBossDefeated;
    }

    private void OnBossDefeated() => Defeated?.Invoke();
}
