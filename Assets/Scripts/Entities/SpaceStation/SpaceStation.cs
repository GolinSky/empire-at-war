using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitWreck;
using EmpireAtWar.Services.UnitExplosion;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Entities.SpaceStation
{
    public class SpaceStation : MonoBehaviour, IController, IInitializable, ILateDisposable
    {
        private IFogOfWarSystem _fogOfWarSystem;
        private ILocalPlayer _localPlayer;
        private IHealthComponent _healthComponent;
        private IUnitExplosionService _explosionService;
        private IUnitWreckService _wreckService;

        [SerializeField] private Renderer[] explosionHullRenderers;
        private EntityComponentLifecycle _componentLifecycle;
        private GameObjectContext _context;

        private PlayerId _owner;
        private Vector3 _startPosition;
        private FactionType _factionType;

        [Inject] private SpaceStationData Data { get; }

        public string Id => GetType().Name;

        [Inject]
        private void Construct(
            IFogOfWarSystem fogOfWarSystem,
            IHealthComponent healthComponent,
            IUnitWreckService wreckService,
            IUnitExplosionService explosionService,
            ILocalPlayer localPlayer,
            List<IMonoComponent> monoComponents,
            GameObjectContext context,
            PlayerId owner,
            Vector3 startPosition,
            FactionType factionType)
        {
            _fogOfWarSystem = fogOfWarSystem;
            _localPlayer = localPlayer;
            _owner = owner;
            _healthComponent = healthComponent;
            _startPosition = startPosition;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents);
            _wreckService = wreckService;
            _explosionService = explosionService;
            _context = context;
            _factionType = factionType;
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            gameObject.name = $"{_owner}_SpaceStation";
            transform.position = _startPosition;

            // Allied stations share vision with the local player.
            if (_localPlayer.IsFriendly(_owner))
            {
                _fogOfWarSystem.RegisterVisionSource(transform, 900f);
            }
        }

        public void LateDispose()
        {
            Release(false);
        }

        private void HandleDestroyed() => Release(true);

        private void Release(bool playDeathAnimation)
        {
            _healthComponent.HealthModelObserver.OnDestroy -= HandleDestroyed;
            if (!_componentLifecycle.Release())
            {
                return;
            }
            if (playDeathAnimation)
            {
                EntityComponentData componentData = Data.ComponentData;
                _explosionService.Spawn(explosionHullRenderers);
                // The explosion hides the swap: the wreck appears as the station entity is destroyed.
                if (Data.TryGetWreck(_factionType, out UnitWreckData wreck))
                {
                    _wreckService.Spawn(wreck, transform, _owner, componentData.DestroyDelay);
                }

                Destroy(_context.gameObject, componentData.DestroyDelay);
            }
        }
    }
}
