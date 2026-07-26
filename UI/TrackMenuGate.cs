using Shared.TrackSelection;
using UnityEngine;

namespace TeaCurses.UI;

/// <summary>
/// True while stock or custom track-select is the active scene controller.
/// </summary>
public static class TrackMenuGate
{
    public static bool IsInTrackMenu()
    {
        var custom = Object.FindObjectOfType<CustomTracksSelectionSceneController>();
        if (custom != null && custom.isActiveAndEnabled)
            return true;

        var stock = Object.FindObjectOfType<TrackSelectionSceneController>();
        return stock != null && stock.isActiveAndEnabled;
    }
}
