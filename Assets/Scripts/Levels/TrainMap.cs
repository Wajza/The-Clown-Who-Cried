using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The train's station-select screen (TrainMap scene). One button per station; a station whose
/// Requirement isn't met yet is greyed out and says "(closed)". Picking a station loads its
/// scene at the station's spawn point.
///
/// Getting here: Tumbleton's station is a Door behind the TaskGate that needs the 4 prologue
/// tasks, so the train only becomes available after the prologue.
///
/// Setup: the TrainMap scene (the level builder makes it). Each button's On Click calls
/// Travel(station number) and is set in the Inspector.
/// </summary>
public class TrainMap : MonoBehaviour
{
    [Serializable]
    public class Station
    {
        public string displayName = "Station";
        public ExitDestination destination = new ExitDestination();
        public ExitRequirement requirement = new ExitRequirement();
        public Button button;
        public TMP_Text label;
    }

    [SerializeField] private List<Station> stations = new List<Station>();

    public IReadOnlyList<Station> Stations => stations;

    public bool IsOpen(int index) => index >= 0 && index < stations.Count && stations[index].requirement.IsMet;

    private void Start()
    {
        Button first = null;
        foreach (Station station in stations)
        {
            bool open = station.requirement.IsMet;
            station.button.interactable = open;
            if (station.label != null)
                station.label.text = open ? station.displayName : $"{station.displayName} (closed)";
            if (open && first == null)
                first = station.button;
        }

        if (first != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    /// <summary>The station buttons call this with their number (0 = first).</summary>
    public void Travel(int index)
    {
        if (!IsOpen(index))
            return;
        stations[index].destination.Go();
    }
}
