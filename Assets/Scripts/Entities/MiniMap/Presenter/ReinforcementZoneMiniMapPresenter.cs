using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.ReinforcementZones;
using Zenject;

namespace EmpireAtWar.Presenters.MiniMap
{
    public sealed class ReinforcementZoneMiniMapPresenter :
        IInitializable, ILateTickable, ILateDisposable, IObserver<BattleState>
    {
        private readonly ReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly MiniMapMarkerCollection<ReinforcementZonePresenter> _markers;
        private readonly INotifier<BattleState> _battleState;

        private bool _hasMarkers;

        public ReinforcementZoneMiniMapPresenter(
            MiniMapData miniMapData,
            ReinforcementZonesSystem reinforcementZonesSystem,
            INotifier<BattleState> battleState)
        {
            _battleState = battleState;
            _markers = new MiniMapMarkerCollection<ReinforcementZonePresenter>(miniMapData);
            _reinforcementZonesSystem = reinforcementZonesSystem;
        }

        public void Initialize()
        {
            _battleState.AddObserver(this);
        }

        // Zones are built from the battle map during loading, so they all exist once the battle runs.
        public void UpdateState(BattleState state)
        {
            if (state != BattleState.Running || _hasMarkers)
            {
                return;
            }

            _hasMarkers = true;
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

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            _markers.Clear();
        }

        public void LateTick()
        {
            foreach (KeyValuePair<ReinforcementZonePresenter, MiniMapMarker> pair in _markers.Pairs)
            {
                RefreshMarker(pair.Key, pair.Value);
            }
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
