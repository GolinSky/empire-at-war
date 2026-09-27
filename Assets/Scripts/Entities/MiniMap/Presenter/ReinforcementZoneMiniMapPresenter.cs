using System.Collections.Generic;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.ReinforcementZones;
using Zenject;

namespace EmpireAtWar.Presenters.MiniMap
{
    public sealed class ReinforcementZoneMiniMapPresenter :
        IInitializable, ILateTickable, ILateDisposable
    {
        private readonly MiniMapData _miniMapData;
        private readonly ReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly Dictionary<ReinforcementZonePresenter, MiniMapMarker> _markers =
            new Dictionary<ReinforcementZonePresenter, MiniMapMarker>();

        public ReinforcementZoneMiniMapPresenter(
            MiniMapData miniMapData,
            ReinforcementZonesSystem reinforcementZonesSystem)
        {
            _miniMapData = miniMapData;
            _reinforcementZonesSystem = reinforcementZonesSystem;
        }

        public void Initialize()
        {
            foreach (ReinforcementZonePresenter zone in _reinforcementZonesSystem.Zones)
            {
                MiniMapMarker marker = new MiniMapMarker(MarkType.ReinforcementZone, zone.Owner);
                _markers.Add(zone, marker);
                RefreshMarker(zone, marker);
                _miniMapData.AddMarker(marker);
            }
        }

        public void LateTick()
        {
            foreach (KeyValuePair<ReinforcementZonePresenter, MiniMapMarker> pair in _markers)
            {
                RefreshMarker(pair.Key, pair.Value);
            }
        }

        public void LateDispose()
        {
            foreach (MiniMapMarker marker in _markers.Values)
            {
                _miniMapData.RemoveMarker(marker);
            }

            _markers.Clear();
        }

        private static void RefreshMarker(
            ReinforcementZonePresenter zone,
            MiniMapMarker marker)
        {
            marker.SetPosition(zone.Center.x, zone.Center.z);
            marker.SetRelation(zone.Owner);
            marker.SetVisible(zone.IsRevealed);
            marker.SetWorldDiameter(zone.Radius * 2f);
        }
    }
}
