using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.BattleNotifications
{
    /// <summary>
    /// Entity-driven cards: own station or mine under attack, station hull critical, own losses (heroes and capital
    /// ships named even off camera, other losses grouped), enemy fleet sighted and enemy station upgrades.
    /// </summary>
    public sealed class ThreatNotifier : IInitializable, ILateDisposable
    {
        private const float CRITICAL_HULL = 0.25f;
        private const float CRITICAL_REARM_HULL = 0.5f;
        private const float FLEET_QUIET_PERIOD = 20f;

        private readonly IBattleNotifications _notifications;
        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _local;
        private readonly FactionCatalog _factions;
        private readonly StationLevelData _stationLevels;
        private readonly Dictionary<IEntity, Subscription> _subscriptions = new Dictionary<IEntity, Subscription>();
        private readonly HashSet<IEntity> _visibleEnemies = new HashSet<IEntity>();
        private float _fleetQuietSince = float.NegativeInfinity;

        public ThreatNotifier(IBattleNotifications notifications, IEntityLocator entities, ILocalPlayer local,
            FactionCatalog factions, StationLevelData stationLevels)
        {
            _notifications = notifications;
            _entities = entities;
            _local = local;
            _factions = factions;
            _stationLevels = stationLevels;
        }

        public void Initialize()
        {
            _entities.EntityAdded += HandleEntityAdded;
            _entities.EntityRemoved += HandleEntityRemoved;
            foreach (IEntity entity in _entities.Entities) HandleEntityAdded(entity);
        }

        public void LateDispose()
        {
            _entities.EntityAdded -= HandleEntityAdded;
            _entities.EntityRemoved -= HandleEntityRemoved;
            foreach (Subscription subscription in _subscriptions.Values) subscription.Dispose();
            _subscriptions.Clear();
            _visibleEnemies.Clear();
        }

        private void HandleEntityAdded(IEntity entity)
        {
            bool isStation = entity.TryGetFacade(out IStationLevelFacade level);
            bool isMine = entity.TryGetFacade(out IMiningFacilityFacade _);
            bool isUnit = entity.TryGetFacade(out IUnitTypeFacade unit);
            bool isOwn = _local.IsLocal(entity.Owner);
            bool isHostile = _local.IsHostile(entity.Owner);
            var subscription = new Subscription();
            if (isOwn && (isStation || isMine)) WatchStructure(entity, subscription, isStation);
            if (isOwn && isUnit) WatchLoss(entity, subscription, unit.UnitTypeId);
            if (isHostile && isStation)
            {
                subscription.Level = level;
                subscription.LevelChanged = newLevel =>
                    _notifications.Post(BattleNotificationPriority.Tactical, $"Enemy station reached level {newLevel}",
                        iconKey: _stationLevels.GetLevelFactionData(newLevel - 1).IconKey);
                level.LevelUpgraded += subscription.LevelChanged;
            }
            if (isHostile && isUnit && unit.UnitTypeId.IsShip && entity.TryGetFacade(out IFogVisibilityFacade fog))
            {
                subscription.Fog = fog;
                if (!fog.IsHiddenByFog) _visibleEnemies.Add(entity);
                subscription.VisibilityChanged = visible => HandleVisibilityChanged(entity, visible);
                fog.VisibilityChanged += subscription.VisibilityChanged;
            }
            if (subscription.IsEmpty) return;
            _subscriptions.Add(entity, subscription);
        }

        private void WatchStructure(IEntity entity, Subscription subscription, bool isStation)
        {
            subscription.Damage = entity.GetFacade<IHealthDamageFacade>();
            // Held, not posted: the card stays while hits keep coming and leaves a lifetime after the last one.
            subscription.Damaged = () =>
            {
                if (entity.HealthModel.IsDestroyed) return;
                if (isStation) _notifications.Hold(BattleNotificationPriority.Urgent, "Station under attack", "attack.station");
                else _notifications.Hold(BattleNotificationPriority.Urgent, "Mining facility under attack", "attack.mine");
            };
            subscription.Damage.Damaged += subscription.Damaged;
            if (!isStation) return;
            subscription.Health = entity.HealthModel;
            subscription.IsCriticalArmed = entity.HealthModel.HullPercentage > CRITICAL_HULL;
            subscription.HealthChanged = () => HandleStationHealthChanged(entity.HealthModel, subscription);
            subscription.Health.OnValueChanged += subscription.HealthChanged;
        }

        private void HandleStationHealthChanged(IHealthModelObserver health, Subscription subscription)
        {
            if (health.IsDestroyed) return;
            float hull = health.HullPercentage;
            if (hull > CRITICAL_REARM_HULL) subscription.IsCriticalArmed = true;
            else if (hull <= CRITICAL_HULL && subscription.IsCriticalArmed)
            {
                subscription.IsCriticalArmed = false;
                _notifications.Post(BattleNotificationPriority.Urgent,
                    $"Station hull critical — {Mathf.CeilToInt(CRITICAL_HULL * 100f)}%");
            }
        }

        private void WatchLoss(IEntity entity, Subscription subscription, UnitTypeId unitType)
        {
            subscription.Health ??= entity.HealthModel;
            subscription.Destroyed = () => PostLoss(entity.HealthModel.ShipClass, unitType);
            subscription.Health.OnDestroy += subscription.Destroyed;
        }

        private void PostLoss(ShipClass shipClass, UnitTypeId unitType)
        {
            FactionData data = unitType.IsShip
                ? _factions.GetShipFactionData(unitType.ShipType)
                : _factions.GetSquadronFactionData(unitType.SquadronType);
            if (data.IsHero || shipClass == ShipClass.Capital || shipClass == ShipClass.HeavyCapital)
                _notifications.Post(BattleNotificationPriority.Urgent, $"{data.Name} destroyed", iconKey: data.IconKey);
            else if (unitType.IsShip)
                _notifications.Post(BattleNotificationPriority.Tactical, $"{data.Name} lost", "loss.ship", "{0} ships lost",
                    iconKey: data.IconKey);
            else
                _notifications.Post(BattleNotificationPriority.Tactical, $"{data.Name} squadron lost", "loss.squadron",
                    "{0} squadrons lost", iconKey: data.IconKey);
        }

        private void HandleVisibilityChanged(IEntity entity, bool visible)
        {
            if (!visible)
            {
                if (_visibleEnemies.Remove(entity) && _visibleEnemies.Count == 0) _fleetQuietSince = Time.unscaledTime;
                return;
            }
            bool isFirst = _visibleEnemies.Count == 0;
            if (_visibleEnemies.Add(entity) && isFirst && Time.unscaledTime - _fleetQuietSince >= FLEET_QUIET_PERIOD)
                _notifications.Post(BattleNotificationPriority.Urgent, "Enemy fleet approaching");
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            if (_subscriptions.Remove(entity, out Subscription subscription)) subscription.Dispose();
            HandleVisibilityChanged(entity, false);
        }

        private sealed class Subscription : IDisposable
        {
            public IStationLevelFacade Level;
            public Action<int> LevelChanged;
            public IHealthDamageFacade Damage;
            public Action Damaged;
            public IHealthModelObserver Health;
            public Action HealthChanged;
            public bool IsCriticalArmed;
            public Action Destroyed;
            public IFogVisibilityFacade Fog;
            public Action<bool> VisibilityChanged;

            public bool IsEmpty => Level == null && Damage == null && Health == null && Fog == null;

            public void Dispose()
            {
                if (Level != null) Level.LevelUpgraded -= LevelChanged;
                if (Damage != null) Damage.Damaged -= Damaged;
                if (HealthChanged != null) Health.OnValueChanged -= HealthChanged;
                if (Destroyed != null) Health.OnDestroy -= Destroyed;
                if (Fog != null) Fog.VisibilityChanged -= VisibilityChanged;
            }
        }
    }
}
