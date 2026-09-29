using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.UnitWreck;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.MiningFacility
{
    public class MiningFacility : MonoBehaviour, IController, IIncomeProvider,
        IInitializable, ILateDisposable
    {
        private IEconomyProvider _economyProvider;
        private IHealthComponent _healthComponent;
        private IRadarComponent _radarComponent;
        private Vector3 _startPosition;
        private EntityComponentLifecycle _componentLifecycle;
        private IUnitWreckService _wreckService;
        private GameObjectContext _context;
        private PlayerId _owner;
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
            IUnitWreckService wreckService,
            GameObjectContext context,
            PlayerId owner,
            ILayerService layerService,
            IFactionResearchModelObserver research)
        {
            _economyProvider = economyProvider;
            _healthComponent = healthComponent;
            _radarComponent = radarComponent;
            _startPosition = startPosition;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents);
            _wreckService = wreckService;
            _context = context;
            _owner = owner;
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
            }

            _research.OnResearchCompleted -= HandleResearchCompleted;
            _economyProvider.RemoveProvider(this);
            if (playDeathEffects)
            {
                OnRelease?.Invoke();
                EntityComponentData componentData = RootModel.ComponentData;
                Instantiate(componentData.DeathExplosionVfx, transform.position, Quaternion.identity);
                // The explosion hides the swap: the wreck appears as the facility entity is destroyed.
                if (RootModel.Wreck != null)
                {
                    _wreckService.Spawn(RootModel.Wreck, transform, _owner, componentData.DestroyDelay);
                }

                Destroy(_context.gameObject, componentData.DestroyDelay);
            }
        }

        private void HandleResearchCompleted(ResearchType researchType)
        {
            _economyProvider.RecalculateIncome(this);
        }
    }
}
