using EmpireAtWar.Entities.BaseEntity.EntityFacades;
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
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.UnitWreck;
using EmpireAtWar.Services.UnitExplosion;
using UnityEngine;
using EmpireAtWar.Lifecycle;
using VContainer;
using LifetimeScope = VContainer.Unity.LifetimeScope;
using VContainer.Internal;

namespace EmpireAtWar.Entities.MiningFacility
{
    public class MiningFacility : EntityView, IController, IIncomeProvider,
        IUnitExplosionFacade, IInitializable, ILateDisposable
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
        private ILayerService _layerService;

        [SerializeField] private Renderer[] explosionHullRenderers;
        private EntityComponentLifecycle _componentLifecycle;
        private LifetimeScope _scope;

        private Vector3 _startPosition;

        public event Action OnRelease;

        private MiningFacilityData Data { get; set; }

        public override string Id => GetType().Name;

        public void PlayExplosion()
        {
            _unitExplosionService.Spawn(explosionHullRenderers);
        }

        /// <summary>One team member's share; every allied economy receives this amount.</summary>
        public float Income => _teamIncome.Income;

        [Inject]
        private void Construct(
            IPlayerRoster playerRoster,
            IPlayerRegistry playerRegistry,
            IHealthComponent healthComponent,
            HealthModel healthModel,
            CombatModifiers combatModifiers,
            float buildTime,
            IUnitWreckService unitWreckService,
            IUnitRenderStateComponent renderState,
            IUnitExplosionService unitExplosionService,
            ILayerService layerService,
            IFactionResearchModelObserver research,
            ContainerLocal<IReadOnlyList<IMonoComponent>> monoComponents,
            LifetimeScope scope,
            Vector3 startPosition,
            PlayerId owner, MiningFacilityData data, MiningFacilityModel rootModel)
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
            Data = data;
            _teamIncome = new TeamIncomeSource(rootModel.BaseIncome, construction, research, playerRoster, playerRegistry, owner);
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            transform.position = _startPosition;
            construction.ProgressChanged += HandleConstructionProgress;
            construction.Begin(_buildTime);
            _teamIncome.Register();
        }

        public void LateDispose()
        {
            Release(false);
        }

        private void HandleDestroyed() => Release(true);

        private void Release(bool playDeathEffects)
        {
            _healthComponent.HealthModelObserver.OnDestroy -= HandleDestroyed;
            construction.ProgressChanged -= HandleConstructionProgress;
            if (!_componentLifecycle.Release())
            {
                return;
            }
            if (playDeathEffects)
            {
                _layerService.Apply(gameObject, LayerKey.Dead, true);
            }

            _teamIncome.Release();
            if (playDeathEffects)
            {
                OnRelease?.Invoke();
                EntityComponentData componentData = Data.ComponentData;
                PlayExplosion();
                // The explosion hides the swap: the wreck appears as the facility entity is destroyed.
                if (Data.Wreck != null)
                {
                    _unitWreckService.Spawn(Data.Wreck, transform, _renderState.WreckUserValue, componentData.DestroyDelay);
                }

                Destroy(_scope.gameObject, componentData.DestroyDelay);
            }
        }

        private void HandleConstructionProgress(float progress)
        {
            _healthModel.SetConstructionProgress(progress);
            _combatModifiers.SetConstructionActive(progress < 1f);
        }
    }
}
