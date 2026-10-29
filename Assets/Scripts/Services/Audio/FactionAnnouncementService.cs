using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.Stations;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    public sealed class FactionAnnouncementService : IInitializable, ITickable, ILateDisposable, IObserver<BattleResult>
    {
        private const float EVENT_COOLDOWN = 1.5f;
        private const float CONSTRUCTION_COOLDOWN = 8f;
        private const float CAP_COOLDOWN = 3f;
        private const float DAMAGE_COOLDOWN = 20f;
        private const float LOSS_COOLDOWN = 8f;
        private const float LOSS_VISIBILITY_MEMORY = 5f;
        private const float FLEET_QUIET_PERIOD = 20f;
        private readonly IInterfaceSfxService _audio;
        private readonly InterfaceSfxData _data;
        private readonly ShipSfxData _ships;
        private readonly FactionService _faction;
        private readonly PlayerFactionModel _model;
        private readonly FactionRoster _roster;
        private readonly ReinforcementService _reinforcements;
        private readonly ReinforcementModel _capacity;
        private readonly ILocalPlayer _local;
        private readonly IProductionAccess _production;
        private readonly IEntityLocator _entities;
        private readonly ICaptureSitesSystem _sites;
        private readonly INotifier<BattleResult> _results;
        private readonly ICameraService _camera;
        private readonly Dictionary<AudioClip, float> _readyAt = new();
        private readonly Dictionary<IEntity, EntitySubscription> _subscriptions = new();
        private readonly HashSet<IEntity> _visibleEnemies = new();
        private FactionAnnouncerSet _clips;
        private bool _capReached;
        private bool _ended;
        private float _captureReadyAt;
        private float _fleetQuietSince = float.NegativeInfinity;

        public FactionAnnouncementService(IInterfaceSfxService audio, InterfaceSfxData data, ShipSfxData ships,
            FactionService faction, PlayerFactionModel model, FactionRoster roster,
            ReinforcementService reinforcements, ReinforcementModel capacity, ILocalPlayer local,
            IProductionAccess production, IEntityLocator entities, ICaptureSitesSystem sites,
            INotifier<BattleResult> results, ICameraService camera)
        {
            _audio = audio;
            _data = data;
            _ships = ships;
            _faction = faction;
            _model = model;
            _roster = roster;
            _reinforcements = reinforcements;
            _capacity = capacity;
            _local = local;
            _production = production;
            _entities = entities;
            _sites = sites;
            _results = results;
            _camera = camera;
        }

        public void Initialize()
        {
            _clips = _data.GetAnnouncer(_model.FactionType);
            _capReached = _capacity.CurrentUnitCapacity >= _capacity.MaxUnitCapacity;
            _faction.PurchaseAccepted += HandlePurchaseAccepted;
            _faction.PurchaseCanceled += HandlePurchaseCanceled;
            _faction.StationUpgraded += HandleStationUpgraded;
            _faction.ResearchCompleted += HandleResearchCompleted;
            _faction.UnitProduced += HandleUnitProduced;
            _reinforcements.ReinforcementDeployed += HandleDeployed;
            _reinforcements.CapacityRejected += HandleCapacityRejected;
            _capacity.OnCapacityChanged += HandleCapacityChanged;
            _sites.SiteCaptured += HandleCaptured;
            _sites.SiteLost += HandleLost;
            _sites.ConstructionStarted += HandleConstructionStarted;
            _sites.ConstructionCompleted += HandleConstructionCompleted;
            _entities.EntityAdded += HandleEntityAdded;
            _entities.EntityRemoved += HandleEntityRemoved;
            _results.AddObserver(this);
            foreach (IEntity entity in _entities.Entities) HandleEntityAdded(entity);
        }

        public void Tick()
        {
            if (_ended) return;
            foreach (EntitySubscription subscription in _subscriptions.Values)
                if (subscription.Health != null && !subscription.Health.IsDestroyed && IsOnCamera(subscription))
                    subscription.LastSeenAt = Time.unscaledTime;
        }

        private bool IsOnCamera(EntitySubscription subscription)
        {
            if (subscription.Fog.IsHiddenByFog) return false;
            Vector3 viewport = _camera.WorldToViewportPoint(subscription.Transform.position);
            return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f &&
                viewport.y >= 0f && viewport.y <= 1f;
        }

        private bool WasRecentlyOnCamera(IEntity entity)
        {
            return _subscriptions.TryGetValue(entity, out EntitySubscription subscription) &&
                (IsOnCamera(subscription) || Time.unscaledTime - subscription.LastSeenAt <= LOSS_VISIBILITY_MEMORY);
        }

        private bool Announce(AudioClip clip, AnnouncementPriority priority, float cooldown = EVENT_COOLDOWN)
        {
            if (_ended || _readyAt.TryGetValue(clip, out float ready) && Time.unscaledTime < ready) return false;
            if (!_audio.TryPlayAnnouncement(clip, priority)) return false;
            _readyAt[clip] = Time.unscaledTime + cooldown;
            return true;
        }

        private void HandlePurchaseAccepted(UnitRequest request)
        {
            if (request is ResearchUnitRequest) Announce(_clips.UpgradeProgress, AnnouncementPriority.Progress);
            else if (request is LevelUnitRequest) Announce(_clips.TechUpgradeProgress, AnnouncementPriority.Progress);
            else if (request is ShipUnitRequest || request is SquadronUnitRequest)
                Announce(_clips.UnitProduction, AnnouncementPriority.Progress);
        }

        private void HandlePurchaseCanceled(UnitRequest request)
        {
            if (request is ResearchUnitRequest) Announce(_clips.UpgradeCanceled, AnnouncementPriority.Progress);
            else if (request is LevelUnitRequest) Announce(_clips.TechUpgradeCanceled, AnnouncementPriority.Progress);
        }

        private void HandleResearchCompleted(ResearchType type) =>
            Announce(_clips.UpgradeComplete, AnnouncementPriority.Notification);

        private void HandleUnitProduced(UnitRequest request)
        {
            AudioClip clip;
            switch (request)
            {
                case ShipUnitRequest ship: clip = _ships.GetUnit(ship.Key).Ready; break;
                case SquadronUnitRequest squadron: clip = _ships.GetUnit(squadron.Key).Ready; break;
                default: return;
            }
            Announce(clip != null ? clip : _clips.ConstructionComplete, AnnouncementPriority.Progress,
                clip != null ? EVENT_COOLDOWN : CONSTRUCTION_COOLDOWN);
        }

        private void HandleStationUpgraded() => AnnounceStationUpgrade(_model.CurrentLevel);

        private void AnnounceStationUpgrade(int level)
        {
            if (_ended || _readyAt.TryGetValue(_clips.StationUpgraded, out float ready) && Time.unscaledTime < ready) return;
            AudioClip unlocked = HasNewConstruction(level) ? _clips.NewConstruction : null;
            if (_audio.TryPlayStationUpgrade(_clips.StationUpgraded, unlocked))
                _readyAt[_clips.StationUpgraded] = Time.unscaledTime + EVENT_COOLDOWN;
        }

        private bool HasNewConstruction(int level)
        {
            foreach (FactionData data in _roster.Ships.Values) if (data.AvailableLevel == level) return true;
            foreach (FactionData data in _roster.Squadrons.Values) if (data.AvailableLevel == level) return true;
            foreach (FactionData data in _roster.MiningFacilities.Values) if (data.AvailableLevel == level) return true;
            foreach (FactionData data in _roster.DefendPlatforms.Values) if (data.AvailableLevel == level) return true;
            foreach (FactionData data in _roster.SuperWeapons.Values) if (data.AvailableLevel == level) return true;
            return false;
        }

        private void HandleDeployed(UnitRequest request)
        {
            if (!(request is ShipUnitRequest) && !(request is SquadronUnitRequest)) return;
            if (!_capReached && _capacity.CurrentUnitCapacity >= _capacity.MaxUnitCapacity)
            {
                _capReached = true;
                HandleCapacityRejected();
                return;
            }
            Announce(_clips.ReinforcementsEnRoute, AnnouncementPriority.Progress);
        }

        private void HandleCapacityRejected() =>
            Announce(_clips.UnitCapReached, AnnouncementPriority.Notification, CAP_COOLDOWN);

        private void HandleCapacityChanged(int capacity)
        {
            if (capacity < _capacity.MaxUnitCapacity) _capReached = false;
        }

        private void HandleConstructionStarted(PlayerId payer, SiteFacilityType type)
        {
            if (payer == _local.Id) Announce(_clips.BuildingStarted, AnnouncementPriority.Progress);
        }

        private void HandleConstructionCompleted(PlayerId owner, SiteFacilityType type)
        {
            if (owner == _local.Id && SiteFacilityBuilder.IsSatellite(type))
                Announce(_clips.ConstructionComplete, AnnouncementPriority.Progress, CONSTRUCTION_COOLDOWN);
        }

        private void HandleCaptured(PlayerId previous, PlayerId owner, SiteFacilityType type)
        {
            if (type != SiteFacilityType.Mining || owner != _local.Id || !previous.IsNone ||
                Time.unscaledTime < _captureReadyAt) return;
            if (Announce(_clips.MiningCaptured, AnnouncementPriority.Notification))
                _captureReadyAt = Time.unscaledTime + EVENT_COOLDOWN;
        }

        private void HandleLost(PlayerId owner, SiteFacilityType type)
        {
            if (owner != _local.Id) return;
            if (type == SiteFacilityType.Mining) Announce(_clips.MiningDestroyed, AnnouncementPriority.Warning);
            else if (SiteFacilityBuilder.IsSatellite(type))
                Announce(_clips.UnitDestroyed, AnnouncementPriority.Notification, LOSS_COOLDOWN);
        }

        private void HandleEntityAdded(IEntity entity)
        {
            bool station = entity.TryGetFacade(out IStationLevelFacade level);
            bool mine = entity.TryGetFacade(out IMiningFacilityFacade mining);
            bool ship = entity.TryGetFacade(out IUnitTypeFacade unit) && unit.UnitTypeId.IsShip;
            bool hostile = _local.IsHostile(entity.Owner);
            if (!station && !mine && !(ship && (hostile || entity.Owner == _local.Id))) return;
            var subscription = new EntitySubscription();
            if (ship && entity.Owner == _local.Id)
            {
                subscription.Transform = entity.GetFacade<IEntityTransformFacade>().Transform;
                subscription.Fog = entity.GetFacade<IFogVisibilityFacade>();
                if (IsOnCamera(subscription)) subscription.LastSeenAt = Time.unscaledTime;
                subscription.Health = entity.HealthModel;
                subscription.Destroyed = () =>
                {
                    if (WasRecentlyOnCamera(entity))
                        Announce(_clips.UnitDestroyed, AnnouncementPriority.Notification, LOSS_COOLDOWN);
                };
                subscription.Health.OnDestroy += subscription.Destroyed;
            }
            if (station)
            {
                subscription.Level = level;
                subscription.LevelChanged = newLevel =>
                {
                    if (entity.Owner == _production.ProductionHost && entity.Owner != _local.Id) AnnounceStationUpgrade(newLevel);
                    else if (_local.IsHostile(entity.Owner)) Announce(_clips.EnemyStationUpgraded, AnnouncementPriority.Notification);
                };
                level.LevelUpgraded += subscription.LevelChanged;
            }
            if ((station || mine) && entity.Owner == _local.Id)
            {
                subscription.Damage = entity.GetFacade<IHealthDamageFacade>();
                subscription.Damaged = () =>
                {
                    if (entity.HealthModel.IsDestroyed || Time.unscaledTime < subscription.DamageReadyAt) return;
                    if (Announce(station ? _clips.StationUnderAttack : _clips.MiningUnderAttack, AnnouncementPriority.Warning))
                        subscription.DamageReadyAt = Time.unscaledTime + DAMAGE_COOLDOWN;
                };
                subscription.Damage.Damaged += subscription.Damaged;
            }
            if (hostile && ship && entity.TryGetFacade(out IFogVisibilityFacade fog))
            {
                subscription.Fog = fog;
                if (!fog.IsHiddenByFog) _visibleEnemies.Add(entity);
                subscription.VisibilityChanged = visible => HandleVisibilityChanged(entity, visible);
                fog.VisibilityChanged += subscription.VisibilityChanged;
            }
            _subscriptions.Add(entity, subscription);
        }

        private void HandleVisibilityChanged(IEntity entity, bool visible)
        {
            if (!visible)
            {
                if (_visibleEnemies.Remove(entity) && _visibleEnemies.Count == 0) _fleetQuietSince = Time.unscaledTime;
                return;
            }
            bool first = _visibleEnemies.Count == 0;
            if (_visibleEnemies.Add(entity) && first && Time.unscaledTime - _fleetQuietSince >= FLEET_QUIET_PERIOD)
                Announce(_clips.EnemyFleetApproaching, AnnouncementPriority.Progress, FLEET_QUIET_PERIOD);
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            if (_subscriptions.Remove(entity, out EntitySubscription subscription)) subscription.Dispose();
            HandleVisibilityChanged(entity, false);
        }

        public void UpdateState(BattleResult result)
        {
            if (_ended || result.Mode == GameMode.BattleSimulation) return;
            if (result.Outcome == BattleOutcome.EnemyVictory)
                _audio.TryPlayAnnouncement(_clips.Defeated, AnnouncementPriority.Defeat);
            _ended = true;
        }

        public void LateDispose()
        {
            _ended = true;
            _faction.PurchaseAccepted -= HandlePurchaseAccepted;
            _faction.PurchaseCanceled -= HandlePurchaseCanceled;
            _faction.StationUpgraded -= HandleStationUpgraded;
            _faction.ResearchCompleted -= HandleResearchCompleted;
            _faction.UnitProduced -= HandleUnitProduced;
            _reinforcements.ReinforcementDeployed -= HandleDeployed;
            _reinforcements.CapacityRejected -= HandleCapacityRejected;
            _capacity.OnCapacityChanged -= HandleCapacityChanged;
            _sites.SiteCaptured -= HandleCaptured;
            _sites.SiteLost -= HandleLost;
            _sites.ConstructionStarted -= HandleConstructionStarted;
            _sites.ConstructionCompleted -= HandleConstructionCompleted;
            _entities.EntityAdded -= HandleEntityAdded;
            _entities.EntityRemoved -= HandleEntityRemoved;
            _results.RemoveObserver(this);
            foreach (EntitySubscription subscription in _subscriptions.Values) subscription.Dispose();
            _subscriptions.Clear();
            _visibleEnemies.Clear();
        }

        private sealed class EntitySubscription : IDisposable
        {
            public IStationLevelFacade Level;
            public Action<int> LevelChanged;
            public IHealthDamageFacade Damage;
            public Action Damaged;
            public float DamageReadyAt;
            public IFogVisibilityFacade Fog;
            public Action<bool> VisibilityChanged;
            public Transform Transform;
            public IHealthModelObserver Health;
            public Action Destroyed;
            public float LastSeenAt = float.NegativeInfinity;

            public void Dispose()
            {
                if (Level != null) Level.LevelUpgraded -= LevelChanged;
                if (Damage != null) Damage.Damaged -= Damaged;
                if (Fog != null) Fog.VisibilityChanged -= VisibilityChanged;
                if (Health != null) Health.OnDestroy -= Destroyed;
            }
        }
    }
}
