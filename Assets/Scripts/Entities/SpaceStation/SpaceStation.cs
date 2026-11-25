using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Weapon;
using UnityEngine.Rendering;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.TeamColor;
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
    public class SpaceStation : EntityView, IController, IUnitExplosionFacade, IInitializable, ILateDisposable
    {
        private const float DESTRUCTION_BURST_SCALE = 0.75f;

        private IVisionService _visionService;
        private IBattleAreaWriter _areas;
        private IHealthComponent _healthComponent;
        private IHealthUpgrade _healthUpgrade;
        private IFactionLevelObserver _factionLevel;
        private IUnitExplosionService _unitExplosionService;
        private IUnitWreckService _unitWreckService;
        private IUnitRenderStateComponent _renderState;
        private IWeaponComponent _weaponComponent;
        private IRadarModelObserver _radarModel;
        private IAttackDataFactory _attackDataFactory;

        [SerializeField] private Renderer[] explosionHullRenderers;
        [Tooltip("Optional for factions that use a distinct model at each station level.")]
        [SerializeField] private StationLevelView levelView;
        private EntityComponentLifecycle _componentLifecycle;
        private LifetimeScope _scope;

        private PlayerId _owner;
        private Vector3 _startPosition;
        private FactionType _factionType;

        private SpaceStationData Data { get; set; }

        /// <summary>Raised once when the station is destroyed in battle.</summary>
        public event System.Action Destroyed;

        public override string Id => GetType().Name;

        public void PlayExplosion(UnitExplosionKind kind)
        {
            _unitExplosionService.Spawn(explosionHullRenderers, kind, DESTRUCTION_BURST_SCALE);
        }

        public BoxCollider HullCollider => levelView.HullCollider;

        [Inject]
        private void Construct(
            IVisionService visionService,
            IBattleAreaWriter areas,
            IHealthComponent healthComponent,
            IHealthUpgrade healthUpgrade,
            IFactionLevelObserver factionLevel,
            IUnitWreckService unitWreckService,
            IUnitRenderStateComponent renderState,
            IUnitExplosionService unitExplosionService,
            IWeaponComponent weaponComponent,
            IRadarModelObserver radarModel,
            IAttackDataFactory attackDataFactory,
            ContainerLocal<IReadOnlyList<IMonoComponent>> monoComponents,
            LifetimeScope scope,
            PlayerId owner,
            Vector3 startPosition,
            FactionType factionType, SpaceStationData data)
        {
            _visionService = visionService;
            _areas = areas;
            _owner = owner;
            _healthComponent = healthComponent;
            _healthUpgrade = healthUpgrade;
            _factionLevel = factionLevel;
            _startPosition = startPosition;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents.Value);
            _unitWreckService = unitWreckService;
            _renderState = renderState;
            _unitExplosionService = unitExplosionService;
            _weaponComponent = weaponComponent;
            _radarModel = radarModel;
            _attackDataFactory = attackDataFactory;
            _scope = scope;
            _factionType = factionType;
            Data = data;
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            _radarModel.Enemies.ItemAdded += HandleEnemyAdded;
            _factionLevel.OnLevelUpgraded += ApplyLevel;
            ApplyLevel(_factionLevel.CurrentLevel);
            gameObject.name = $"{_owner}_SpaceStation";
            transform.position = _startPosition;
            _visionService.Register(_owner, transform, Data.ComponentData.VisionRange);
            _areas.Register(BattleAreaLayer.SpawnBlock, _owner, transform, Data.ComponentData.SpawnBlockRadius);
        }

        public void LateDispose()
        {
            Release(false);
        }

        private void HandleDestroyed()
        {
            Release(true);
            Destroyed?.Invoke();
        }

        // Every detected enemy becomes a base target for the station's weapons.
        private void HandleEnemyAdded(ObservableList<IEntity> sender, ListChangedEventArgs<IEntity> args)
        {
            IEntity enemy = args.item;
            if (enemy.HealthModel.IsDestroyed || !enemy.HealthModel.HasUnits) return;
            _weaponComponent.AddTarget(_attackDataFactory.ConstructData(enemy), AttackType.Base);
        }

        // Each level toughens the station, installs that level's hardpoints and restores destroyed ones.
        private void ApplyLevel(int level)
        {
            if (levelView != null)
            {
                levelView.ApplyLevel(level);
                explosionHullRenderers = levelView.CurrentModel.HullRenderers;
            }
            StationLevelStats stats = Data.GetLevelStats(level);
            _healthUpgrade.Upgrade(level, stats.HullMultiplier, stats.ShieldsMultiplier, stats.ShieldRegenerateMultiplier,
                stats.HullMultiplier);
        }

        private void Release(bool playDeathAnimation)
        {
            _healthComponent.HealthModelObserver.OnDestroy -= HandleDestroyed;
            _radarModel.Enemies.ItemAdded -= HandleEnemyAdded;
            _factionLevel.OnLevelUpgraded -= ApplyLevel;
            if (!_componentLifecycle.Release())
            {
                return;
            }
            _visionService.Unregister(transform);
            _areas.Unregister(BattleAreaLayer.SpawnBlock, transform);
            if (playDeathAnimation)
            {
                EntityComponentData componentData = Data.ComponentData;
                PlayExplosion(UnitExplosionKind.Destruction);
                // The explosion hides the swap: the wreck appears as the station entity is destroyed.
                if (Data.TryGetWrecks(_factionType, out StationLevelWrecks wrecks))
                {
                    _unitWreckService.Spawn(wrecks.Get(_factionLevel.CurrentLevel), transform, _renderState.WreckUserValue,
                        componentData.DestroyDelay);
                }

                Destroy(_scope.gameObject, componentData.DestroyDelay);
            }
        }
    }
}
