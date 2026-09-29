using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitWreck;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Entities.SpaceStation
{
    public class SpaceStation : MonoBehaviour, IController, IInitializable, ILateDisposable
    {
        private FogOfWarSystem _fogOfWarSystem;
        private ILocalPlayer _localPlayer;
        private PlayerId _owner;
        private IHealthComponent _healthComponent;
        private IRadarComponent _radarComponent;
        private Vector3 _startPosition;
        private EntityComponentLifecycle _componentLifecycle;
        private IUnitWreckService _wreckService;
        private GameObjectContext _context;
        private FactionType _factionType;

        [Inject] private SpaceStationData RootModel { get; }

        public string Id => GetType().Name;

        [Inject]
        private void Construct(
            FogOfWarSystem fogOfWarSystem,
            PlayerId owner,
            IHealthComponent healthComponent,
            IRadarComponent radarComponent,
            Vector3 startPosition,
            List<IMonoComponent> monoComponents,
            IUnitWreckService wreckService,
            GameObjectContext context,
            FactionType factionType,
            ILocalPlayer localPlayer)
        {
            _fogOfWarSystem = fogOfWarSystem;
            _localPlayer = localPlayer;
            _owner = owner;
            _healthComponent = healthComponent;
            _radarComponent = radarComponent;
            _startPosition = startPosition;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents);
            _wreckService = wreckService;
            _context = context;
            _factionType = factionType;
        }

        public IModel GetModel()
        {
            return RootModel;
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            gameObject.name = $"{_owner}_SpaceStation";
            transform.position = _startPosition;
            _radarComponent.SetPosition(transform.position);

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
                EntityComponentData componentData = RootModel.ComponentData;
                Instantiate(componentData.DeathExplosionVfx, transform.position, Quaternion.identity);
                // The explosion hides the swap: the wreck appears as the station entity is destroyed.
                if (RootModel.TryGetWreck(_factionType, out UnitWreckData wreck))
                {
                    _wreckService.Spawn(wreck, transform, _owner, componentData.DestroyDelay);
                }

                Destroy(_context.gameObject, componentData.DestroyDelay);
            }
        }
    }
}
