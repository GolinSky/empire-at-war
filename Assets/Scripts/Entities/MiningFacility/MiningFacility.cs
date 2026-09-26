using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.UnitDeathAnimation;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.MiningFacility
{
    public interface IMiningFacilityCommand : ICommand
    {
    }

    public class MiningFacility : MonoBehaviour, IController, IMiningFacilityCommand, IIncomeProvider,
        IInitializable, ILateDisposable
    {
        private IEconomyProvider _economyProvider;
        private IHealthComponent _healthComponent;
        private IRadarComponent _radarComponent;
        private Vector3 _startPosition;
        private EntityComponentLifecycle _componentLifecycle;
        private IUnitDeathAnimationData _deathAnimationData;
        private IUnitDeathAnimationService _deathAnimationService;
        private ILayerService _layerService;
        private IFactionResearchModelObserver _research;

        [Inject] private MiningFacilityData RootModel { get; }

        public event Action OnRelease;

        public string Id => GetType().Name;
        public float Income => RootModel.Income * _research.IncomeMultiplier;

        [Inject]
        private void Construct(
            IEconomyProvider economyProvider,
            IHealthComponent healthComponent,
            IRadarComponent radarComponent,
            Vector3 startPosition,
            List<IMonoComponent> monoComponents,
            IUnitDeathAnimationData deathAnimationData,
            IUnitDeathAnimationService deathAnimationService,
            ILayerService layerService,
            IFactionResearchModelObserver research)
        {
            _economyProvider = economyProvider;
            _healthComponent = healthComponent;
            _radarComponent = radarComponent;
            _startPosition = startPosition;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents);
            _deathAnimationData = deathAnimationData;
            _deathAnimationService = deathAnimationService;
            _layerService = layerService;
            _research = research;
        }

        public IModel GetModel()
        {
            return RootModel;
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            transform.position = _startPosition;
            _radarComponent.SetPosition(transform.position);
            _economyProvider.AddProvider(this);
            _research.OnResearchCompleted += HandleResearchCompleted;
        }

        public void LateDispose()
        {
            Release(false);
        }

        private void HandleDestroyed() => Release(true);

        private void Release(bool playDeathEffects)
        {
            _healthComponent.HealthModelObserver.OnDestroy -= HandleDestroyed;
            if (!_componentLifecycle.Release())
            {
                return;
            }
            if (playDeathEffects)
            {
                _layerService.Apply(gameObject, LayerKey.Dead, true);
                _deathAnimationService.Play(transform, _deathAnimationData);
            }

            _research.OnResearchCompleted -= HandleResearchCompleted;
            _economyProvider.RemoveProvider(this);
            if (playDeathEffects)
            {
                OnRelease?.Invoke();
            }
        }

        private void HandleResearchCompleted(ResearchType researchType)
        {
            _economyProvider.RecalculateIncome(this);
        }
    }
}
