using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Opens and closes the arena's trapdoor sections in cycles. Each phase has its own settings
/// (by default: phase 1 off, phase 2 slow, phase 3 fast), so Pip must use the platforms later on.
///
/// One cycle: stay closed -> warn the chosen sections (blink, at least 0.5 s) -> open them -> close.
/// At least one section always stays closed. All timings follow the boss's difficulty.
/// When Chortle is defeated, every section closes and stays closed.
/// </summary>
public class TrapdoorFloor : MonoBehaviour
{
    [Serializable]
    public class PhaseSettings
    {
        [Tooltip("Do trapdoors open in this phase?")]
        public bool active;
        [Min(0f), Tooltip("Seconds everything stays closed between openings.")]
        public float closedTime = 4f;
        [Min(EnemyBase.MinWarningTime), Tooltip("Warning blink before opening (seconds, at least 0.5).")]
        public float warningTime = 0.8f;
        [Min(0f), Tooltip("Seconds the sections stay open.")]
        public float openTime = 2f;
        [Range(0f, 1f), Tooltip("Share of sections that open each time (at least 1, never all).")]
        public float openShare = 0.4f;
    }

    [SerializeField] private ChortleBoss boss;
    [SerializeField] private List<TrapdoorSection> sections = new List<TrapdoorSection>();

    [Header("Per phase")]
    [SerializeField] private PhaseSettings phase1 = new PhaseSettings { active = false };
    [SerializeField] private PhaseSettings phase2 = new PhaseSettings { active = true, closedTime = 5f, warningTime = 0.8f, openTime = 2f, openShare = 0.34f };
    [SerializeField] private PhaseSettings phase3 = new PhaseSettings { active = true, closedTime = 2f, warningTime = 0.6f, openTime = 1.5f, openShare = 0.5f };

    private Coroutine _cycle;

    private void OnEnable()
    {
        if (boss == null)
            return;
        boss.PhaseChanged += OnPhaseChanged;
        boss.OnDefeated += StopAndClose;
    }

    private void OnDisable()
    {
        if (boss == null)
            return;
        boss.PhaseChanged -= OnPhaseChanged;
        boss.OnDefeated -= StopAndClose;
    }

    private void OnPhaseChanged(int phase)
    {
        StopAndClose();

        PhaseSettings settings = phase switch { 1 => phase1, 2 => phase2, _ => phase3 };
        if (settings.active && sections.Count > 0)
            _cycle = StartCoroutine(Cycle(settings));
    }

    private void StopAndClose()
    {
        if (_cycle != null)
            StopCoroutine(_cycle);
        _cycle = null;

        foreach (TrapdoorSection section in sections)
            section.SetOpen(false);
    }

    private IEnumerator Cycle(PhaseSettings s)
    {
        var chosen = new List<TrapdoorSection>();

        while (true)
        {
            yield return new WaitForSeconds(boss.ScaleTime(s.closedTime));

            // Pick which sections open this time.
            ChooseSections(s.openShare, chosen);

            // Warn, open, wait, close.
            float warning = boss.ScaleWarning(s.warningTime);
            foreach (TrapdoorSection section in chosen)
                section.Warn(warning);
            yield return new WaitForSeconds(warning);

            foreach (TrapdoorSection section in chosen)
                section.SetOpen(true);
            yield return new WaitForSeconds(boss.ScaleTime(s.openTime));

            foreach (TrapdoorSection section in chosen)
                section.SetOpen(false);
        }
    }

    private void ChooseSections(float share, List<TrapdoorSection> result)
    {
        result.Clear();

        // At least 1 opens; if there is more than one section, at least 1 stays closed.
        int count = Mathf.Clamp(Mathf.RoundToInt(sections.Count * share), 1, Mathf.Max(1, sections.Count - 1));

        var pool = new List<TrapdoorSection>(sections);
        for (int i = 0; i < count; i++)
        {
            int pick = UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[pick]);
            pool.RemoveAt(pick);
        }
    }
}
