using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
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
    public class DefendPlatform : EntityView, IController, IIncomeProvider, IInitializable, ILateDisposable
    {
        private IPlayerRoster _playerRoster;
        private IPlayerRegistry _playerRegistry;
        private IFactionResearchModelObserver _research;
        private readonly List<IEconomyProvider> _teamEconomies = new();
        private IHealthComponent _healthComponent;
        private IUnitExplosionService _unitExplosionService;
        private IUnitWreckService _unitWreckService;
        private ILayerService _layerService;

        [SerializeField] private Renderer[] explosionHullRenderers;
        private EntityComponentLifecycle _componentLifecycle;
        private LifetimeScope _scope;

        private Vector3 _startPosition;
        private PlayerId _owner;

        public event Action OnRelease;

        private DefendPlatformData Data { get; set; }

        public override string Id => GetType().Name;
        /// <summary>One team member's share; every allied economy receives this amount.</summary>
        public float Income => Data.Income * _research.IncomeMultiplier / _teamEconomies.Count;

        [Inject]
        private void Construct(
            IPlayerRoster playerRoster,
            IPlayerRegistry playerRegistry,
            IFactionResearchModelObserver research,
            IHealthComponent healthComponent,
            IUnitWreckService unitWreckService,
            IUnitExplosionService unitExplosionService,
            ILayerService layerService,
            ContainerLocal<IReadOnlyList<IMonoComponent>> monoComponents,
            LifetimeScope scope,
            Vector3 startPosition,
            PlayerId owner, DefendPlatformData data)
        {
            _playerRoster = playerRoster;
            _playerRegistry = playerRegistry;
            _research = research;
            _healthComponent = healthComponent;
            _startPosition = startPosition;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents.Value);
            _unitWreckService = unitWreckService;
            _unitExplosionService = unitExplosionService;
            _scope = scope;
            _owner = owner;
            _layerService = layerService;
            Data = data;
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            transform.position = _startPosition;
            if (Data.Income > 0f)
            {
                foreach (PlayerSlot player in _playerRoster.Players)
                {
                    if (_playerRoster.IsAllied(_owner, player.Id))
                    {
                        _teamEconomies.Add(_playerRegistry.GetEconomy(player.Id));
                    }
                }
                // Registered only once the team is known, so every economy totals the final share.
                foreach (IEconomyProvider economy in _teamEconomies)
                {
                    economy.AddProvider(this);
                }
                _research.OnResearchCompleted += HandleResearchCompleted;
            }
        }

        public void LateDispose()
        {
            Release(false);
        }

        private void Release(bool playDeathEffects)
        {
            _healthComponent.HealthModelObserver.OnDestroy -= HandleDestroyed;
            if (!_componentLifecycle.Release())
            {
                return;
            }
            if (Data.Income > 0f)
            {
                _research.OnResearchCompleted -= HandleResearchCompleted;
                foreach (IEconomyProvider economy in _teamEconomies)
                {
                    economy.RemoveProvider(this);
                }
                _teamEconomies.Clear();
            }
            if (playDeathEffects)
            {
                _layerService.Apply(gameObject, LayerKey.Dead, true);
                OnRelease?.Invoke();
                EntityComponentData componentData = Data.ComponentData;
                _unitExplosionService.Spawn(explosionHullRenderers);
                // The explosion hides the swap: the wreck appears as the platform entity is destroyed.
                if (Data.Wreck != null)
                {
                    _unitWreckService.Spawn(Data.Wreck, transform, _owner, componentData.DestroyDelay);
                }

                Destroy(_scope.gameObject, componentData.DestroyDelay);
            }
        }

        private void HandleDestroyed() => Release(true);

        private void HandleResearchCompleted(ResearchType researchType)
        {
            foreach (IEconomyProvider economy in _teamEconomies)
            {
                economy.RecalculateIncome(this);
            }
        }
    }
}
