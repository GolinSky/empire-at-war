using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.TacticalMarkers;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class AlliedMarkerHintCondition : IHintCondition
    {
        private readonly ITacticalMarkerObserver _markers;
        private readonly ILocalPlayer _local;
        public bool IsMet { get; private set; }

        public AlliedMarkerHintCondition(ITacticalMarkerObserver markers, ILocalPlayer local)
        {
            _markers = markers; _local = local;
            _markers.Added += Added;
            foreach (TacticalMarker marker in _markers.Markers) Added(marker);
        }

        private void Added(TacticalMarker marker)
        { if (!_local.IsLocal(marker.Sender) && _local.IsFriendly(marker.Sender)) IsMet = true; }

        public void Dispose() => _markers.Added -= Added;
    }
}
