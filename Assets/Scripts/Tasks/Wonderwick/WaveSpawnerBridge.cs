using UnityEngine;

/// <summary>
/// Connects a task step to my teammate's WaveSpawner (W3 "Runaway Coaster", after the 4 switches):
///   - when the task reaches Start At Step, it calls WaveSpawner.StartWaves(),
///   - when the spawner says OnAllWavesCleared, it sends the task signal (e.g. "w3_waves").
///
/// The WaveSpawner itself (spawn points, which enemies in which wave) is my teammate's script.
/// Untick its "Start On Player Enter" so only this bridge starts it.
/// TODO (teammate, TEAMMATE_TODO #9): fill in the waves with enemy prefabs. With no waves, the
/// spawner counts as cleared at once, so the task still works before the enemies exist.
/// </summary>
public class WaveSpawnerBridge : MonoBehaviour
{
    [SerializeField] private WaveSpawner spawner;
    [SerializeField] private TaskData task;
    [SerializeField, Tooltip("Which objective (0 = first) starts the waves.")]
    private int startAtStep = 1;
    [SerializeField] private string signalKey = "w3_waves";

    private void OnEnable()
    {
        GameEvents.TaskProgressed += OnTaskChanged;
        GameEvents.TaskStarted += OnTaskChanged;
        GameEvents.SceneLoaded += OnSceneLoaded;
        if (spawner != null)
            spawner.OnAllWavesCleared += OnCleared;
    }

    private void OnDisable()
    {
        GameEvents.TaskProgressed -= OnTaskChanged;
        GameEvents.TaskStarted -= OnTaskChanged;
        GameEvents.SceneLoaded -= OnSceneLoaded;
        if (spawner != null)
            spawner.OnAllWavesCleared -= OnCleared;
    }

    private void OnTaskChanged(TaskData changed)
    {
        if (changed == task)
            TryStart();
    }

    // Coming back to the level in the middle of the task: start the waves again.
    private void OnSceneLoaded(string sceneName, string spawnPointId) => TryStart();

    private void TryStart()
    {
        TaskManager manager = TaskManager.Instance;
        if (spawner == null || manager == null || manager.GetStep(task) != startAtStep)
            return;
        if (!spawner.IsRunning && !spawner.IsCleared)
            spawner.StartWaves();
    }

    private void OnCleared() => GameEvents.RaiseTaskSignal(signalKey);
}
