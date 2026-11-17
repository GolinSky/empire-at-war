using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class GravityWellAbility : IStoppableShipAbility, IReplicaPersistentShipAbility
    {
        private readonly IBattleAreaWriter _areas;
        private readonly GravityWellEffects _effects;
        private readonly GravityWellSettings _settings;
        private IShipAbilityFacade _caster;
        private Transform _casterTransform;
        private IGravityWellEmitterFacade _emitter;
        private GravityWellView _view;

        public GravityWellAbility(IBattleAreaWriter areas, GravityWellEffects effects, GravityWellSettings settings)
        {
            _areas = areas;
            _effects = effects;
            _settings = settings;
        }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            Stop();
            _caster = caster;
            _casterTransform = caster.Entity.GetFacade<IEntityTransformFacade>().Transform;
            _emitter = caster.Entity.GetFacade<IGravityWellEmitterFacade>();
            _caster.Health.OnDestroy += Stop;
            _caster.Modifiers.AddMovementAnchor();
            _effects.StartEmitter(_emitter);
            _areas.Register(BattleAreaLayer.SpawnBlock, caster.Entity.Owner, _casterTransform, definition.Range);
            _view = _effects.Show(_settings.ViewPrefab, caster.Entity.Owner, _casterTransform, definition.Range,
                caster.Entity.GetFacade<IEntitySelectionObserverFacade>().Selection);
        }

        public void Stop()
        {
            if (_caster != null)
            {
                _caster.Health.OnDestroy -= Stop;
                _caster.Modifiers.RemoveMovementAnchor();
                _effects.StopEmitter(_emitter);
                _emitter = null;
                _caster = null;
            }
            if (_casterTransform != null)
            {
                _areas.Unregister(BattleAreaLayer.SpawnBlock, _casterTransform);
                _casterTransform = null;
            }
            if (_view != null)
            {
                _effects.Hide(_view);
                _view = null;
            }
        }
    }
}
