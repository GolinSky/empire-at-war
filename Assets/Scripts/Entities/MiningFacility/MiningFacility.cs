using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitDeathAnimation;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.MiningFacility
{
    public interface IMiningFacilityCommand : ICommand
    {
    }

    public class MiningFacility : MonoBehaviour, IController, IMiningFacilityCommand, IIncomeProvider,
        IInitializable, ILateDisposable, IEntityLifecycle
    {
        private IEconomyProvider _economyProvider;
        private IHealthComponent _healthComponent;
        private IRadarComponent _radarComponent;
        private Vector3 _startPosition;
        private IReadOnlyList<IMonoComponent> _monoComponents;
        private IUnitDeathAnimationData _deathAnimationData;
        private IUnitDeathAnimationService _deathAnimationService;
        private bool _isReleased;

        [Inject] private MiningFacilityModel RootModel { get; }

        public event Action OnRelease;

        public string Id => GetType().Name;
        public float Income => RootModel.Income;

        [Inject]
        private void Construct(
            IEconomyProvider economyProvider,
            IHealthComponent healthComponent,
            IRadarComponent radarComponent,
            Vector3 startPosition,
            List<IMonoComponent> monoComponents,
            IUnitDeathAnimationData deathAnimationData,
            IUnitDeathAnimationService deathAnimationService)
        {
            _economyProvider = economyProvider;
            _healthComponent = healthComponent;
            _radarComponent = radarComponent;
            _startPosition = startPosition;
            _monoComponents = monoComponents;
            _deathAnimationData = deathAnimationData;
            _deathAnimationService = deathAnimationService;
        }

        public IModel GetModel()
        {
            return RootModel;
        }

        public void Initialize()
        {
            transform.position = _startPosition;
            _healthComponent.SetMovementState(false);
            _radarComponent.SetPosition(transform.position);
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroy;
            _economyProvider.AddProvider(this);
        }

        public void LateDispose()
        {
            Release(false);
        }

        public void Release()
        {
            Release(true);
        }

        private void Release(bool playDeathEffects)
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            foreach (IMonoComponent component in _monoComponents)
            {
                component.Release();
            }
            if (playDeathEffects)
            {
                _deathAnimationService.Play(transform, _deathAnimationData);
            }

            _healthComponent.HealthModelObserver.OnDestroy -= HandleDestroy;
            _economyProvider.RemoveProvider(this);
            if (playDeathEffects)
            {
                OnRelease?.Invoke();
            }
        }

        private void HandleDestroy()
        {
            _economyProvider.RemoveProvider(this);
        }
    }
}
