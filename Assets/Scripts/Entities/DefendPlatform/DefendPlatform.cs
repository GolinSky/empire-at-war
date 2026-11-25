using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Weapon;
using UnityEngine.Rendering;
using System;
using EmpireAtWar.Components.Construction;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Components.Combat;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitWreck;
using EmpireAtWar.Services.UnitExplosion;
using UnityEngine;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Lifecycle;
using VContainer;
using LifetimeScope = VContainer.Unity.LifetimeScope;
using VContainer.Internal;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class DefendPlatform : EntityView, IController, IIncomeProvider, IUnitExplosionFacade, IInitializable, ILateDisposable
    {
        private TeamIncomeSource _teamIncome;
        private IHealthComponent _healthComponent;
        private HealthModel _healthModel;
        private CombatModifiers _combatModifiers;
        private float _buildTime;
        [SerializeField] private ConstructionComponent construction;

        public ConstructionComponent Construction => construction;
        public float ConstructionProgress => construction.Progress;
        public bool IsConstructing => construction.IsConstructing;
        public event Action<float> ConstructionProgressChanged
        {
            add => construction.ProgressChanged += value;
            remove => construction.ProgressChanged -= value;
        }

        private IUnitExplosionService _unitExplosionService;
        private IUnitWreckService _unitWreckService;
        private IUnitRenderStateComponent _renderState;
        private IWeaponComponent _weaponComponent;
        private IRadarModelObserver _radarModel;
        private IAttackDataFactory _attackDataFactory;
        private ILayerService _layerService;

        [SerializeField] private Renderer[] explosionHullRenderers;
        private EntityComponentLifecycle _componentLifecycle;
        private LifetimeScope _scope;

        private Vector3 _startPosition;

        public event Action OnRelease;

        private DefendPlatformData Data { get; set; }

        public override string Id => GetType().Name;

        public void PlayExplosion(UnitExplosionKind kind)
        {
            _unitExplosionService.Spawn(explosionHullRenderers, kind);
        }

        /// <summary>One team member's share; every allied economy receives this amount.</summary>
        public float Income => _teamIncome.Income;

        [Inject]
        private void Construct(
            IPlayerRoster playerRoster,
            IPlayerRegistry playerRegistry,
            IFactionResearchModelObserver research,
            IHealthComponent healthComponent,
            HealthModel healthModel,
            CombatModifiers combatModifiers,
            float buildTime,
            IUnitWreckService unitWreckService,
            IUnitRenderStateComponent renderState,
            IUnitExplosionService unitExplosionService,
            ILayerService layerService,
            IWeaponComponent weaponComponent,
            IRadarModelObserver radarModel,
            IAttackDataFactory attackDataFactory,
            ContainerLocal<IReadOnlyList<IMonoComponent>> monoComponents,
            LifetimeScope scope,
            Vector3 startPosition,
            PlayerId owner, DefendPlatformData data)
        {
            _healthComponent = healthComponent;
            _healthModel = healthModel;
            _combatModifiers = combatModifiers;
            _buildTime = buildTime;
            _startPosition = startPosition;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents.Value);
            _unitWreckService = unitWreckService;
            _renderState = renderState;
            _unitExplosionService = unitExplosionService;
            _scope = scope;
            _layerService = layerService;
            _weaponComponent = weaponComponent;
            _radarModel = radarModel;
            _attackDataFactory = attackDataFactory;
            Data = data;
            _teamIncome = new TeamIncomeSource(data.Income, construction, research, playerRoster, playerRegistry, owner);
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            _radarModel.Enemies.ItemAdded += HandleEnemyAdded;
            transform.position = _startPosition;
            construction.ProgressChanged += HandleConstructionProgress;
            construction.Begin(_buildTime);
            _teamIncome.Register();
        }

        public void LateDispose()
        {
            Release(false);
        }

        private void Release(bool playDeathEffects)
        {
            _healthComponent.HealthModelObserver.OnDestroy -= HandleDestroyed;
            _radarModel.Enemies.ItemAdded -= HandleEnemyAdded;
            construction.ProgressChanged -= HandleConstructionProgress;
            if (!_componentLifecycle.Release())
            {
                return;
            }
            _teamIncome.Release();
            if (playDeathEffects)
            {
                _layerService.Apply(gameObject, LayerKey.Dead, true);
                OnRelease?.Invoke();
                EntityComponentData componentData = Data.ComponentData;
                PlayExplosion(UnitExplosionKind.Destruction);
                // The explosion hides the swap: the wreck appears as the platform entity is destroyed.
                if (Data.Wreck != null)
                {
                    _unitWreckService.Spawn(Data.Wreck, transform, _renderState.WreckUserValue, componentData.DestroyDelay);
                }

                Destroy(_scope.gameObject, componentData.DestroyDelay);
            }
        }

        private void HandleDestroyed() => Release(true);

        // Every detected enemy becomes a base target for the station's weapons.
        private void HandleEnemyAdded(ObservableList<IEntity> sender, ListChangedEventArgs<IEntity> args)
        {
            IEntity enemy = args.item;
            if (enemy.HealthModel.IsDestroyed || !enemy.HealthModel.HasUnits) return;
            _weaponComponent.AddTarget(_attackDataFactory.ConstructData(enemy), AttackType.Base);
        }

        private void HandleConstructionProgress(float progress)
        {
            _healthModel.SetConstructionProgress(progress);
            _combatModifiers.SetConstructionActive(progress < 1f);
        }
    }
}
