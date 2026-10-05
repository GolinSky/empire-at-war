using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Presenters.MiniMap
{
    /// <summary>
    /// Marks every space station. Friendly stations are always shown; a hostile station appears only once the
    /// local team's vision reaches its footprint, and then stays as known terrain.
    /// </summary>
    public sealed class BaseMiniMapPresenter : IInitializable, ILateTickable, ILateDisposable, IObserver<BattleMap>
    {
        private readonly IVisionService _vision;
        private readonly ILocalPlayer _localPlayer;
        private readonly IPlayerRoster _playerRoster;
        private readonly INotifier<BattleMap> _battleMap;
        private readonly MapGenerationSettings _settings;

        private readonly MiniMapMarkerCollection<PlayerId> _markers;
        private readonly List<(PlayerSlot Player, MiniMapMarker Marker)> _undiscovered =
            new List<(PlayerSlot Player, MiniMapMarker Marker)>();
        private MapLayout _layout;

        public BaseMiniMapPresenter(
            IVisionService vision,
            ILocalPlayer localPlayer,
            IPlayerRoster playerRoster,
            INotifier<BattleMap> battleMap,
            MapGenerationSettings settings,
            MiniMapData miniMapData)
        {
            _vision = vision;
            _localPlayer = localPlayer;
            _playerRoster = playerRoster;
            _battleMap = battleMap;
            _settings = settings;
            _markers = new MiniMapMarkerCollection<PlayerId>(miniMapData);
        }

        public void Initialize() => _battleMap.AddObserver(this);

        public void LateDispose()
        {
            _battleMap.RemoveObserver(this);
            _markers.Clear();
        }

        public void UpdateState(BattleMap battleMap)
        {
            _layout = battleMap.Layout;
            foreach (PlayerSlot player in _playerRoster.Players)
            {
                bool isHostile = _localPlayer.IsHostile(player.Id);
                MiniMapMarker marker = new MiniMapMarker(isHostile ? MarkType.EnemyBase : MarkType.PlayerBase, player.Id);
                Vector3 position = _layout.GetStationPosition(player.Id);
                marker.SetPosition(position.x, position.z);
                marker.SetVisible(!isHostile);
                _markers.Add(player.Id, marker);
                if (isHostile) _undiscovered.Add((player, marker));
            }
        }

        public void LateTick()
        {
            for (int i = _undiscovered.Count - 1; i >= 0; i--)
            {
                (PlayerSlot player, MiniMapMarker marker) = _undiscovered[i];
                if (!_vision.IsAreaVisible(_localPlayer.Id, _layout.GetStationPosition(player.Id),
                        _settings.GetStationRadius(player.Faction)))
                {
                    continue;
                }

                marker.SetVisible(true);
                _undiscovered.RemoveAt(i);
            }
        }
    }
}
