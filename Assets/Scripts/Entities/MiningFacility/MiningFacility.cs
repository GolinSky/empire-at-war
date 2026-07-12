using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Mvc;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.MiningFacility
{
    public interface IMiningFacilityCommand : ICommand
    {
    }

    public class MiningFacility : View<IMiningFacilityModelObserver>, IController, IMiningFacilityCommand,
        IIncomeProvider
    {
        private IEconomyProvider _economyProvider;
        private IHealthComponent _healthComponent;
        private IRadarComponent _radarComponent;
        private Vector3 _startPosition;

        [Inject] private MiningFacilityModel RootModel { get; }

        public string Id => GetType().Name;
        public float Income => RootModel.Income;

        [Inject]
        private void Construct(
            IEconomyProvider economyProvider,
            IHealthComponent healthComponent,
            IRadarComponent radarComponent,
            Vector3 startPosition)
        {
            _economyProvider = economyProvider;
            _healthComponent = healthComponent;
            _radarComponent = radarComponent;
            _startPosition = startPosition;
        }

        public IModel GetModel()
        {
            return RootModel;
        }

        protected override void OnInitialize()
        {
            transform.position = _startPosition;
            _healthComponent.SetMovementState(false);
            _radarComponent.SetPosition(transform.position);
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroy;
            _economyProvider.AddProvider(this);
        }

        protected override void OnDispose()
        {
            _healthComponent.HealthModelObserver.OnDestroy -= HandleDestroy;
            _economyProvider.RemoveProvider(this);
        }

        private void HandleDestroy()
        {
            _economyProvider.RemoveProvider(this);
        }
    }
}
