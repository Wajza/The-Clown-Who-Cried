using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// A quick check of a BUILT game (the .exe), not the Editor. It does nothing unless the game is
/// started with the "-smoketest" command-line argument, so players never see it:
///
///   TheClownWhoCried.exe -batchmode -nographics -smoketest -logFile smoketest.log
///
/// It plays: title screen -> New Game (Pip's house, opening skipped) -> Wonderwick -> the Laugh
/// Tower (intro skipped, boss there) -> End of Demo, and checks the things a build can lose:
/// scenes in the build, Resources (GameSettings/Pip, tasks, sound library), and that test-only
/// content was left out. Prints "SMOKETEST PASS" or "SMOKETEST FAIL: ..." and quits (exit code
/// 0 or 1). Any real save file is moved aside first and put back afterwards.
/// </summary>
public static class BuildSmokeTest
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartIfAsked()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-smoketest") < 0)
            return;
        var go = new GameObject("BuildSmokeTest");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<Runner>();
    }

    private class Runner : MonoBehaviour
    {
        private string _scene;
        private string _failure;
        private string _backup;

        private void OnEnable() => GameEvents.SceneLoaded += (scene, _) => _scene = scene;

        // Watchdog: an exception inside a nested step stops the coroutine without reaching the
        // code above, so quit from here if the run takes far too long.
        private void Update()
        {
            if (Time.realtimeSinceStartup > 150f)
            {
                SaveSystem.Delete();
                if (_backup != null && File.Exists(_backup)) File.Move(_backup, SaveSystem.SavePath);
                Debug.Log("SMOKETEST FAIL: stuck (see the exception above)");
                Application.Quit(1);
            }
        }

        private IEnumerator Start()
        {
            _backup = SaveSystem.SavePath + ".before-smoketest";
            if (File.Exists(_backup)) File.Delete(_backup);
            if (File.Exists(SaveSystem.SavePath)) File.Move(SaveSystem.SavePath, _backup);

            float deadline = Time.realtimeSinceStartup + 90f;
            IEnumerator steps = Steps();
            while (_failure == null)
            {
                if (Time.realtimeSinceStartup > deadline) { _failure = "timed out"; break; }
                bool more;
                try { more = steps.MoveNext(); }
                catch (Exception e) { _failure = e.ToString(); break; }
                if (!more) break;
                yield return steps.Current;
            }

            SaveSystem.Delete();
            if (File.Exists(_backup)) File.Move(_backup, SaveSystem.SavePath);

            Debug.Log(_failure == null ? "SMOKETEST PASS" : $"SMOKETEST FAIL: {_failure}");
            Application.Quit(_failure == null ? 0 : 1);
        }

        private IEnumerator Steps()
        {
            yield return Arrive("Title", 15f);
            Require(GameSettings.Instance != null && GameSettings.Instance.playerPrefab != null, "GameSettings/Pip prefab missing from Resources");
            Require(AudioManager.Instance != null && AudioManager.Instance.Library != null, "SoundLibrary missing");
            Require(AudioManager.Instance.CurrentMusicId == SoundIds.MusicTitle, "no title music");
            Require(TaskManager.Instance.FindTask("p1_wake_up") != null && TaskManager.Instance.FindTask("w4_audition") != null, "story tasks missing from Resources");
            Require(TaskManager.Instance.FindTask("test_errand") == null, "test tasks were shipped");

            Require(GameManager.Instance.NewGame(), "New Game refused");
            yield return Arrive("Tumbleton_PipHouse", 15f);
            CutscenePlayer.Instance.Skip();
            yield return new WaitForSeconds(1f);
            Require(PlayerController.Current != null, "no Pip in Pip's house");
            Require(Mathf.Abs(PlayerController.Current.transform.position.y) < 0.2f, "Pip isn't standing on the floor");
            Require(TaskManager.Instance.IsActive(TaskManager.Instance.FindTask("p1_wake_up")), "P1 didn't start");

            SceneLoader.Instance.LoadScene("Wonderwick_Hub", "FromTrain");
            yield return Arrive("Wonderwick_Hub", 15f);
            Require(PlayerController.Current != null && CameraRig.Current != null, "hub: no Pip or camera");

            SceneLoader.Instance.LoadScene("ChortleArena", "Start");
            yield return Arrive("ChortleArena", 15f);
            Require(CutscenePlayer.Instance.IsPlaying, "no boss intro");
            CutscenePlayer.Instance.Skip();
            yield return null;
            Require(BossHealthSource.Active != null && BossHealthSource.Active.IsFighting, "the boss fight didn't start");

            SceneLoader.Instance.LoadScene("EndOfDemo");
            yield return Arrive("EndOfDemo", 15f);
            Require(FindAnyObjectByType<EndOfDemoSequence>() != null, "no End of Demo sequence");
        }

        private IEnumerator Arrive(string scene, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while ((_scene != scene || SceneLoader.Instance == null || SceneLoader.Instance.IsLoading) && Time.realtimeSinceStartup < end)
                yield return null;
            Require(_scene == scene, $"never reached '{scene}'");
            Debug.Log($"SMOKETEST reached {scene}");
        }

        private void Require(bool ok, string what)
        {
            if (!ok && _failure == null)
                _failure = what;
        }
    }
}
