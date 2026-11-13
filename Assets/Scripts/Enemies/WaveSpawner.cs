using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Spawns waves of enemies in a room (an arena fight).
///
/// - Each wave lists enemy prefabs. They appear at the spawn points in turn
///   (1st enemy at point 1, 2nd at point 2, ..., wrapping around).
/// - The next wave starts once every enemy of the current wave has popped.
/// - When the last wave is cleared, OnAllWavesCleared fires (open the doors, play a jingle...).
///
/// Starting: tick "Start On Player Enter" and give this object a trigger collider covering
/// the room, or call StartWaves() from another script. It only ever runs once.
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Serializable]
    public class Wave
    {
        [Tooltip("Enemy prefabs in this wave.")]
        public List<EnemyBase> enemies = new List<EnemyBase>();
        [Min(0f), Tooltip("Pause before this wave starts (seconds).")]
        public float delayBefore = 1f;
        [Min(0f), Tooltip("Pause between each enemy appearing (seconds). 0 = all at once.")]
        public float spawnInterval = 0.3f;
    }

    [SerializeField, Tooltip("Where enemies appear. Place empty child objects at floor level.")]
    private Transform[] spawnPoints;
    [SerializeField] private List<Wave> waves = new List<Wave>();
    [SerializeField, Tooltip("Start when Pip enters this object's trigger collider.")]
    private bool startOnPlayerEnter = true;
    [SerializeField, Tooltip("Optional effect shown where each enemy appears (e.g. a puff of smoke).")]
    private GameObject spawnEffectPrefab;

    [Header("Events (hook up doors, music, etc.)")]
    [SerializeField] private UnityEvent onWavesStarted;
    [SerializeField] private UnityEvent onAllWavesClearedInspector;

    /// <summary>Fired when the first wave begins.</summary>
    public event Action WavesStarted;

    /// <summary>Fired with the wave index (0 = first) when each wave starts.</summary>
    public event Action<int> WaveStarted;

    /// <summary>Fired once when every wave has been cleared.</summary>
    public event Action OnAllWavesCleared;

    public bool IsRunning { get; private set; }
    public bool IsCleared { get; private set; }

    /// <summary>Index of the current wave (0-based), or -1 before starting.</summary>
    public int CurrentWave { get; private set; } = -1;

    private readonly List<EnemyBase> _alive = new List<EnemyBase>();
    private int _nextSpawnPoint;

    /// <summary>Starts the fight. Does nothing if it is already running or finished.</summary>
    public void StartWaves()
    {
        if (IsRunning || IsCleared)
            return;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("WaveSpawner: add at least one spawn point.", this);
            return;
        }

        StartCoroutine(RunWaves());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (startOnPlayerEnter && other.GetComponentInParent<PlayerController>() != null)
            StartWaves();
    }

    private IEnumerator RunWaves()
    {
        IsRunning = true;
        onWavesStarted?.Invoke();
        WavesStarted?.Invoke();

        for (int i = 0; i < waves.Count; i++)
        {
            Wave wave = waves[i];
            CurrentWave = i;
            yield return new WaitForSeconds(wave.delayBefore);
            WaveStarted?.Invoke(i);

            // Spawn every enemy of this wave.
            foreach (EnemyBase prefab in wave.enemies)
            {
                if (prefab != null)
                    Spawn(prefab);
                if (wave.spawnInterval > 0f)
                    yield return new WaitForSeconds(wave.spawnInterval);
            }

            // Wait until they have all popped (or were destroyed some other way).
            while (_alive.Count > 0)
            {
                _alive.RemoveAll(enemy => enemy == null || enemy.IsPopped);
                yield return null;
            }
        }

        IsRunning = false;
        IsCleared = true;
        onAllWavesClearedInspector?.Invoke();
        OnAllWavesCleared?.Invoke();
    }

    private void Spawn(EnemyBase prefab)
    {
        Transform point = spawnPoints[_nextSpawnPoint % spawnPoints.Length];
        _nextSpawnPoint++;

        if (spawnEffectPrefab != null)
            Instantiate(spawnEffectPrefab, point.position, Quaternion.identity);

        EnemyBase enemy = Instantiate(prefab, point.position, Quaternion.identity);
        enemy.Popped += OnEnemyPopped;
        _alive.Add(enemy);
    }

    private void OnEnemyPopped(EnemyBase enemy)
    {
        enemy.Popped -= OnEnemyPopped;
        _alive.Remove(enemy);
        Destroy(enemy.gameObject, 0.1f); // Popped enemies are already hidden; clean them up.
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null)
            return;

        Gizmos.color = Color.magenta;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
                Gizmos.DrawWireCube(point.position + Vector3.up * 0.5f, new Vector3(1f, 1f, 0f));
        }
    }
}
