using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One task (a quest), written in the Inspector. Example:
///   Id:          bakery
///   Title:       The Flat Cake
///   Description: The baker's cake won't rise. Maybe the mayor knows why?
///   Objectives:  1. Talk to the mayor   2. Talk to the baker      (done IN ORDER)
///
/// WHERE TO PUT TASK ASSETS
/// In Assets/Resources/Tasks (subfolders are fine). TaskManager loads every task in that
/// folder at start, so a saved game can find its tasks again by id. A task outside that
/// folder still works when something starts it, but Continue can't restore it.
///
/// FLAGS THE TASK SETS (so NPCs, gates and cutscenes can react without new code)
///   task_[id]_active   while the task is running   (removed when it's done)
///   task_[id]_done     once it's finished
///   + "Set Flag When Complete", if you fill it in.
///
/// Create one: Create > Clown Who Cried > Tasks > Task.
/// </summary>
[CreateAssetMenu(menuName = "Clown Who Cried/Tasks/Task", fileName = "Task_")]
public class TaskData : ScriptableObject
{
    /// <summary>Which city's Task Board (in the Journal) the task is listed on.</summary>
    public enum City { Tumbleton, Wonderwick }

    [Tooltip("Unique id used in the save file and in flags. Short, lowercase, no spaces, e.g. 'bakery'. Don't change it once saves exist.")]
    public string id;

    [Tooltip("Name shown in the tracker, e.g. 'The Flat Cake'.")]
    public string title;

    [TextArea(2, 4), Tooltip("Longer explanation (for a task list screen later).")]
    public string description;

    [Tooltip("The steps, done in order. Drag objective assets here.")]
    public List<TaskObjective> objectives = new List<TaskObjective>();

    [Tooltip("Optional extra flag set when the task is finished.")]
    public string setFlagWhenComplete;

    [Header("Rewards")]
    [Tooltip("Abilities Pip gets when the task STARTS (e.g. the mallet for P4 Reflex Check).")]
    public List<Ability> unlockOnStart = new List<Ability>();

    [Tooltip("Abilities Pip gets when the task is FINISHED (e.g. Juggle Shot for W3).")]
    public List<Ability> unlockOnComplete = new List<Ability>();

    [Header("Journal")]
    [Tooltip("Which city's Task Board lists this task.")]
    public City city = City.Tumbleton;

    [Tooltip("Position on the Task Board (lower = higher up).")]
    public int boardOrder;

    [Tooltip("Untick to keep the task off the Task Board (e.g. hidden or test tasks).")]
    public bool showOnTaskBoard = true;

    [Tooltip("Only for automatic tests: hidden from the Journal unless a test asks for it.")]
    public bool testOnly;

    /// <summary>Flag set while the task is running.</summary>
    public string ActiveFlag => $"task_{id}_active";

    /// <summary>Flag set once the task is finished.</summary>
    public string DoneFlag => $"task_{id}_done";

    private void OnValidate()
    {
        // A missing id would break saving, so fall back to the asset's name.
        if (string.IsNullOrWhiteSpace(id))
            id = name;
    }
}
