using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Shared machinery for my automatic Play-mode tests (CoreSystemsTest, DialogueSystemTest...).
/// A test scene contains one test driver; pressing Play runs it.
///
/// What the base class does, so each test only has to describe its steps:
///   - Check(label, condition): prints PASS/FAIL in the Console and remembers the result.
///   - WaitUntil(condition, seconds, what): waits for something, FAILs if it takes too long.
///   - A time limit for the whole test, and catching unexpected exceptions as a FAIL.
///   - Moves your real save file aside first and puts it back afterwards.
///   - Writes Logs/[ResultsFileName] and, when Unity was started from the command line
///     (batch mode), closes Unity with exit code 0 (all passed) or 1 (something failed).
/// </summary>
public abstract class SystemTestBase : MonoBehaviour
{
    [SerializeField, Tooltip("Run the test automatically when Play starts. Untick to just walk around.")]
    private bool runOnStart = true;

    [SerializeField, Min(5f), Tooltip("Give up (and fail) if the whole test takes longer than this (seconds).")]
    private float timeout = 120f;

    /// <summary>The steps of the test.</summary>
    protected abstract IEnumerator RunTest();

    /// <summary>Name of the results file in the Logs folder, e.g. "Part1TestResults.txt".</summary>
    protected abstract string ResultsFileName { get; }

    /// <summary>Short tag printed in front of every Console line, e.g. "Part1Test".</summary>
    protected abstract string LogTag { get; }

    private readonly List<string> _results = new List<string>();
    private int _failures;

    // Test types currently running (see Start).
    private static readonly HashSet<Type> s_runningTests = new HashSet<Type>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => s_runningTests.Clear();

    // ------------------------------------------------------------------ Unity lifecycle

    protected virtual void Start()
    {
        if (!runOnStart)
            return;

        // If the test reloads its own scene, the scene's copy of this driver must not start a
        // second run: the first driver (kept alive below) is still going.
        if (!s_runningTests.Add(GetType()))
        {
            Destroy(gameObject);
            return;
        }

        // Survive scene loads, in case the test travels between scenes.
        DontDestroyOnLoad(gameObject);
        StartCoroutine(RunWithTimeout());
    }

    // ------------------------------------------------------------------ Running

    private IEnumerator RunWithTimeout()
    {
        string backup = SaveSystem.SavePath + ".before-test";
        BackUpRealSave(backup);

        IEnumerator test = RunTest();
        float deadline = Time.realtimeSinceStartup + timeout;
        bool finished = false;

        // Step through the test by hand so we can stop it if it runs too long or throws.
        while (true)
        {
            if (Time.realtimeSinceStartup > deadline)
            {
                Fail($"Timed out after {timeout} s");
                break;
            }

            bool hasMore;
            try { hasMore = test.MoveNext(); }
            catch (Exception e)
            {
                Fail($"Exception: {e}");
                break;
            }

            if (!hasMore)
            {
                finished = true;
                break;
            }
            yield return test.Current;
        }

        RestoreRealSave(backup);
        Finish(finished);
    }

    // ------------------------------------------------------------------ Helpers for tests

    protected void Check(string label, bool ok)
    {
        if (!ok)
            _failures++;
        string line = (ok ? "PASS  " : "FAIL  ") + label;
        _results.Add(line);
        if (ok) Debug.Log($"[{LogTag}] {line}");
        else Debug.LogError($"[{LogTag}] {line}");
    }

    protected void Fail(string reason) => Check(reason, false);

    /// <summary>Pip's feet must be on the expected point (x within 0.15 tiles, y within 0.1).</summary>
    protected void CheckNear(string label, Vector2 actual, Vector2 expected)
    {
        bool ok = Mathf.Abs(actual.x - expected.x) < 0.15f && Mathf.Abs(actual.y - expected.y) < 0.1f;
        Check($"{label} (at {actual}, expected {expected})", ok);
    }

    protected IEnumerator WaitUntil(Func<bool> condition, float seconds, string what)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > end)
            {
                Fail($"Waited {seconds} s for {what}");
                yield break;
            }
            yield return null;
        }
    }

    protected static Vector2 PipPosition() => PlayerController.Current.transform.position;

    // ------------------------------------------------------------------ Shared steps (dialogue tests)

    /// <summary>Height of the floor in the test scenes (Part1Setup.FloorY is editor-only).</summary>
    protected const float TestFloorY = -1.5f;

    protected static DialogueRunner Runner => DialogueRunner.Instance;

    /// <summary>Puts Pip on the floor at x (and floor height y) and waits for triggers and InteractionInput to notice.</summary>
    protected IEnumerator MovePipTo(float x, float y = TestFloorY)
    {
        PlayerController.Current.Teleport(new Vector2(x, y), 0f);
        yield return new WaitForSeconds(0.15f); // A few physics steps for the triggers.
        yield return null;                      // One Update for InteractionInput.
    }

    /// <summary>Finish typing the current line (if needed), then move on.</summary>
    protected IEnumerator CompleteAndAdvance()
    {
        if (Runner.IsTyping)
        {
            Runner.RequestAdvance();
            yield return null;
        }
        Runner.RequestAdvance();
        yield return null;
    }

    /// <summary>Press Advance until the choices appear (or the conversation ends).</summary>
    protected IEnumerator AdvanceToChoices()
    {
        for (int i = 0; i < 40 && Runner.IsRunning && !Runner.IsWaitingForChoice; i++)
            yield return CompleteAndAdvance();
    }

    /// <summary>Press Advance until the conversation is over (picking the first answer if asked).</summary>
    protected IEnumerator FinishConversation()
    {
        for (int i = 0; i < 40 && Runner.IsRunning; i++)
            yield return CompleteAndAdvance();
        Check("Conversation could be finished", !Runner.IsRunning);
    }

    // ------------------------------------------------------------------ Results

    private void Finish(bool finished)
    {
        if (!finished && _failures == 0)
            Fail("Test did not finish");

        var sb = new StringBuilder();
        sb.AppendLine($"{LogTag} - {DateTime.Now}");
        foreach (string line in _results)
            sb.AppendLine(line);
        string summary = _failures == 0 ? $"ALL PASSED ({_results.Count} checks)" : $"{_failures} FAILED";
        sb.AppendLine(summary);

        string path = Path.Combine(Application.dataPath, "..", "Logs", ResultsFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[{LogTag}] {summary}. Results: {path}");

#if UNITY_EDITOR
        // Only close Unity when it was started from the command line.
        if (Application.isBatchMode)
            UnityEditor.EditorApplication.Exit(_failures == 0 ? 0 : 1);
#endif
    }

    private static void BackUpRealSave(string backup)
    {
        if (File.Exists(backup))
            File.Delete(backup);
        if (File.Exists(SaveSystem.SavePath))
            File.Move(SaveSystem.SavePath, backup);
    }

    private static void RestoreRealSave(string backup)
    {
        SaveSystem.Delete(); // Remove the test's save.
        if (File.Exists(backup))
            File.Move(backup, SaveSystem.SavePath);
    }
}
