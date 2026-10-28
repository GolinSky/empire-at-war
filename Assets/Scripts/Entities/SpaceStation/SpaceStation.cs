using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitWreck;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.UnitExplosion;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.ViewComponents.Station;
using UnityEngine;
using EmpireAtWar.Lifecycle;
using VContainer;
using LifetimeScope = VContainer.Unity.LifetimeScope;
using VContainer.Internal;

namespace EmpireAtWar.Entities.SpaceStation
{
    public class SpaceStation : EntityView, IController, IInitializable, ILateDisposable
    {
        private IVisionService _visionService;
        private ISpawnBlockerService _spawnBlockerService;
        private IHealthComponent _healthComponent;
        private IHealthUpgrade _healthUpgrade;
        private IIonFieldShape _ionField;
        private IFactionLevelObserver _factionLevel;
        private IUnitExplosionService _unitExplosionService;
        private IUnitWreckService _unitWreckService;

        [SerializeField] private Renderer[] explosionHullRenderers;
        [Tooltip("Optional for factions that use a distinct model at each station level.")]
        [SerializeField] private StationLevelView levelView;
        private EntityComponentLifecycle _componentLifecycle;
        private LifetimeScope _scope;

        private PlayerId _owner;
        private Vector3 _startPosition;
        private FactionType _factionType;

        private SpaceStationData Data { get; set; }

        public override string Id => GetType().Name;

        [Inject]
        private void Construct(
            IVisionService visionService,
            ISpawnBlockerService spawnBlockerService,
            IHealthComponent healthComponent,
            IHealthUpgrade healthUpgrade,
            IIonFieldShape ionField,
            IFactionLevelObserver factionLevel,
            IUnitWreckService unitWreckService,
            IUnitExplosionService unitExplosionService,
            ContainerLocal<IReadOnlyList<IMonoComponent>> monoComponents,
            LifetimeScope scope,
            PlayerId owner,
            Vector3 startPosition,
            FactionType factionType, SpaceStationData data)
        {
            _visionService = visionService;
            _spawnBlockerService = spawnBlockerService;
            _owner = owner;
            _healthComponent = healthComponent;
            _healthUpgrade = healthUpgrade;
            _ionField = ionField;
            _factionLevel = factionLevel;
            _startPosition = startPosition;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents.Value);
            _unitWreckService = unitWreckService;
            _unitExplosionService = unitExplosionService;
            _scope = scope;
            _factionType = factionType;
            Data = data;
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            _factionLevel.OnLevelUpgraded += ApplyLevel;
            ApplyLevel(_factionLevel.CurrentLevel);
            gameObject.name = $"{_owner}_SpaceStation";
            transform.position = _startPosition;
            _visionService.Register(_owner, transform, Data.ComponentData.VisionRange);
            _spawnBlockerService.Register(_owner, transform, Data.ComponentData.SpawnBlockRadius);
        }

        public void LateDispose()
        {
            Release(false);
        }

        private void HandleDestroyed() => Release(true);

        // Each level toughens the station, installs that level's hardpoints and restores destroyed ones.
        private void ApplyLevel(int level)
        {
            if (levelView != null)
            {
                levelView.ApplyLevel(level);
                explosionHullRenderers = levelView.CurrentModel.HullRenderers;
                _ionField.SetIonFieldBounds(levelView.CurrentModel.HullBounds);
            }
            StationLevelStats stats = Data.GetLevelStats(level);
            _healthUpgrade.Upgrade(level, stats.HullMultiplier, stats.ShieldsMultiplier, stats.ShieldRegenerateMultiplier,
                stats.HullMultiplier);
        }

        private void Release(bool playDeathAnimation)
        {
            _healthComponent.HealthModelObserver.OnDestroy -= HandleDestroyed;
            _factionLevel.OnLevelUpgraded -= ApplyLevel;
            if (!_componentLifecycle.Release())
            {
                return;
            }
            _visionService.Unregister(transform);
            _spawnBlockerService.Unregister(transform);
            if (playDeathAnimation)
            {
                EntityComponentData componentData = Data.ComponentData;
                _unitExplosionService.Spawn(explosionHullRenderers);
                // The explosion hides the swap: the wreck appears as the station entity is destroyed.
                if (Data.TryGetWreck(_factionType, out UnitWreckData wreck))
                {
                    _unitWreckService.Spawn(wreck, transform, _owner, componentData.DestroyDelay);
                }

                Destroy(_scope.gameObject, componentData.DestroyDelay);
            }
        }
    }
}
