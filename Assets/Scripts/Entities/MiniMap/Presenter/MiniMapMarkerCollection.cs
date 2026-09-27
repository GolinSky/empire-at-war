using System.Collections.Generic;
using EmpireAtWar.Models.MiniMap;

namespace EmpireAtWar.Presenters.MiniMap
{
    public sealed class MiniMapMarkerCollection<T>
    {
        private readonly MiniMapData _miniMapData;
        private readonly Dictionary<T, MiniMapMarker> _markers = new Dictionary<T, MiniMapMarker>();

        public MiniMapMarkerCollection(MiniMapData miniMapData)
        {
            _miniMapData = miniMapData;
        }

        public IEnumerable<KeyValuePair<T, MiniMapMarker>> Pairs => _markers;

        public void Add(T source, MiniMapMarker marker)
        {
            _markers.Add(source, marker);
            _miniMapData.AddMarker(marker);
        }

        public void Clear()
        {
            foreach (MiniMapMarker marker in _markers.Values)
            {
                _miniMapData.RemoveMarker(marker);
            }

            _markers.Clear();
        }
    }
}
