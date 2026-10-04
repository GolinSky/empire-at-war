using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.UnitWreck;
using EmpireAtWar.Services.UnitExplosion;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.MiningFacility
{
    public class MiningFacility : MonoBehaviour, IController, IIncomeProvider,
        IInitializable, ILateDisposable
    {
        private IEconomyProvider _economyProvider;
        private IHealthComponent _healthComponent;
        private IUnitExplosionService _unitExplosionService;
        private IUnitWreckService _unitWreckService;
        private ILayerService _layerService;
        private IFactionResearchModelObserver _research;

        [SerializeField] private Renderer[] explosionHullRenderers;
        private EntityComponentLifecycle _componentLifecycle;
        private GameObjectContext _context;

        private Vector3 _startPosition;
        private PlayerId _owner;

        public event Action OnRelease;

        [Inject] private MiningFacilityData Data { get; }
        [Inject] private MiningFacilityModel RootModel { get; }

        public string Id => GetType().Name;
        public float Income => RootModel.BaseIncome * _research.IncomeMultiplier;

        [Inject]
        private void Construct(
            IEconomyProvider economyProvider,
            IHealthComponent healthComponent,
            IUnitWreckService unitWreckService,
            IUnitExplosionService unitExplosionService,
            ILayerService layerService,
            IFactionResearchModelObserver research,
            List<IMonoComponent> monoComponents,
            GameObjectContext context,
            Vector3 startPosition,
            PlayerId owner)
        {
            _economyProvider = economyProvider;
            _healthComponent = healthComponent;
            _startPosition = startPosition;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents);
            _unitWreckService = unitWreckService;
            _unitExplosionService = unitExplosionService;
            _context = context;
            _owner = owner;
            _layerService = layerService;
            _research = research;
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            transform.position = _startPosition;
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
                EntityComponentData componentData = Data.ComponentData;
                _unitExplosionService.Spawn(explosionHullRenderers);
                // The explosion hides the swap: the wreck appears as the facility entity is destroyed.
                if (Data.Wreck != null)
                {
                    _unitWreckService.Spawn(Data.Wreck, transform, _owner, componentData.DestroyDelay);
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
