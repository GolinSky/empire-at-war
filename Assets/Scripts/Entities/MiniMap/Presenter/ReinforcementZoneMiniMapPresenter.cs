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
        private readonly ReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly MiniMapMarkerCollection<ReinforcementZonePresenter> _markers;

        public ReinforcementZoneMiniMapPresenter(
            MiniMapData miniMapData,
            ReinforcementZonesSystem reinforcementZonesSystem)
        {
            _markers = new MiniMapMarkerCollection<ReinforcementZonePresenter>(miniMapData);
            _reinforcementZonesSystem = reinforcementZonesSystem;
        }

        public void Initialize()
        {
            foreach (ReinforcementZonePresenter zone in _reinforcementZonesSystem.Zones)
            {
                MiniMapMarker marker = new MiniMapMarker(MarkType.ReinforcementZone, zone.Owner);
                // Zones are known terrain: always drawn, with the owner from the last sighting.
                marker.SetPosition(zone.Center.x, zone.Center.z);
                marker.SetWorldDiameter(zone.Radius * 2f);
                marker.SetVisible(true);
                _markers.Add(zone, marker);
            }
        }

        public void LateTick()
        {
            foreach (KeyValuePair<ReinforcementZonePresenter, MiniMapMarker> pair in _markers.Pairs)
            {
                RefreshMarker(pair.Key, pair.Value);
            }
        }

        public void LateDispose()
        {
            _markers.Clear();
        }

        private static void RefreshMarker(
            ReinforcementZonePresenter zone,
            MiniMapMarker marker)
        {
            if (zone.IsRevealed)
            {
                marker.SetOwner(zone.Owner);
            }
        }
    }
}
