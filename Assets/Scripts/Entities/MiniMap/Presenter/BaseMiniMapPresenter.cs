using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Units;
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
    /// local team's vision reaches its footprint, and then stays until the station is destroyed.
    /// </summary>
    public sealed class BaseMiniMapPresenter : IInitializable, ILateTickable, ILateDisposable, IObserver<BattleMap>
    {
        private readonly IVisionService _vision;
        private readonly ILocalPlayer _localPlayer;
        private readonly IPlayerRoster _playerRoster;
        private readonly INotifier<BattleMap> _battleMap;
        private readonly MapGenerationSettings _settings;
        private readonly IEntityLocator _entityLocator;
        private readonly Dictionary<IEntity, Action> _destroyHandlers = new Dictionary<IEntity, Action>();

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
            MiniMapData miniMapData,
            IEntityLocator entityLocator)
        {
            _vision = vision;
            _localPlayer = localPlayer;
            _playerRoster = playerRoster;
            _battleMap = battleMap;
            _settings = settings;
            _entityLocator = entityLocator;
            _markers = new MiniMapMarkerCollection<PlayerId>(miniMapData);
        }

        public void Initialize()
        {
            _entityLocator.EntityAdded += HandleEntityAdded;
            _entityLocator.EntityRemoved += HandleEntityRemoved;
            foreach (IEntity entity in _entityLocator.Entities)
            {
                HandleEntityAdded(entity);
            }
            _battleMap.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleMap.RemoveObserver(this);
            _entityLocator.EntityAdded -= HandleEntityAdded;
            _entityLocator.EntityRemoved -= HandleEntityRemoved;
            foreach (KeyValuePair<IEntity, Action> handler in _destroyHandlers)
            {
                handler.Key.HealthModel.OnDestroy -= handler.Value;
            }
            _destroyHandlers.Clear();
            _undiscovered.Clear();
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
            foreach (IEntity station in _destroyHandlers.Keys)
            {
                if (station.HealthModel.IsDestroyed) RemoveStationMarker(station.Owner);
            }
        }

        private void HandleEntityAdded(IEntity entity)
        {
            if (!entity.IsPlayerBase()) return;
            Action handler = () => RemoveStationMarker(entity.Owner);
            _destroyHandlers.Add(entity, handler);
            entity.HealthModel.OnDestroy += handler;
            if (entity.HealthModel.IsDestroyed) RemoveStationMarker(entity.Owner);
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            if (!_destroyHandlers.TryGetValue(entity, out Action handler)) return;
            entity.HealthModel.OnDestroy -= handler;
            _destroyHandlers.Remove(entity);
            RemoveStationMarker(entity.Owner);
        }

        private void RemoveStationMarker(PlayerId owner)
        {
            _undiscovered.RemoveAll(entry => entry.Player.Id == owner);
            _markers.Remove(owner);
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
