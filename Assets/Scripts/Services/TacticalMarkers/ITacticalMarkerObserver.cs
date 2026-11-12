using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.TacticalMarkers
{
    public interface ITacticalMarkerObserver
    {
        IReadOnlyList<TacticalMarker> Markers { get; }
        float Time { get; }
        event Action<TacticalMarker> Added;
        event Action<TacticalMarker> Updated;
        event Action<TacticalMarker> Removed;
    }
}
