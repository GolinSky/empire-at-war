using System.Collections.Generic;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.TacticalMarkers;

namespace EmpireAtWar.Entities.TacticalMarkers
{
    public sealed class MiniMapMarkerPresenter : IInitializable, ILateDisposable
    {
        private readonly ITacticalMarkerObserver _source;
        private readonly MiniMapData _map;
        private readonly ILocalPlayer _local;
        private readonly Dictionary<long, MiniMapMarker> _markers = new Dictionary<long, MiniMapMarker>();

        public MiniMapMarkerPresenter(ITacticalMarkerObserver source, MiniMapData map, ILocalPlayer local)
        { _source = source; _map = map; _local = local; }

        public void Initialize()
        {
            _source.Added += Add; _source.Updated += Update; _source.Removed += Remove;
            foreach (TacticalMarker marker in _source.Markers) Add(marker);
        }

        private void Add(TacticalMarker marker)
        {
            if (!_local.IsFriendly(marker.Sender)) return;
            var mapMarker = new MiniMapMarker((MarkType)((int)MarkType.TacticalLook + (int)marker.Type), marker.Sender);
            mapMarker.SetPosition(marker.Point.x, marker.Point.z); mapMarker.SetVisible(true);
            _markers.Add(marker.Id, mapMarker); _map.AddMarker(mapMarker);
        }

        private void Update(TacticalMarker marker)
        {
            if (_markers.TryGetValue(marker.Id, out MiniMapMarker mapMarker))
                mapMarker.SetPosition(marker.Point.x, marker.Point.z);
        }

        private void Remove(TacticalMarker marker)
        { if (_markers.Remove(marker.Id, out MiniMapMarker mapMarker)) _map.RemoveMarker(mapMarker); }

        public void LateDispose()
        {
            _source.Added -= Add; _source.Updated -= Update; _source.Removed -= Remove;
            foreach (MiniMapMarker marker in _markers.Values) _map.RemoveMarker(marker);
            _markers.Clear();
        }
    }
}
