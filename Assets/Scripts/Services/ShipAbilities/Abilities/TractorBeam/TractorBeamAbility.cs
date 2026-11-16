using System.Linq;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class TractorBeamAbility : ICompletableShipAbility, ITargetedShipAbility, IReplicaPersistentShipAbility
    {
        private readonly TractorBeamSettings _settings;
        private readonly CombatStatModifier _movementPenalty;
        private IShipAbilityFacade _caster;
        private IEntity _target;
        public IEntity Target => _target;
        private IHardPointModel _hardPoint;
        private CombatModifiers _targetModifiers;
        private CombatModifierHandle _applied;
        private Transform _targetTransform;
        private BeamShot _view;
        private float _range;

        public bool IsComplete => _hardPoint.IsDestroyed || _target.HealthModel.IsDestroyed || _target.IsCloaked() || _targetTransform == null ||
            _caster.Modifiers.AbilitiesSuppressed ||
            !ShipAbilityRange.IsInRange(_caster, _target, _range);

        public TractorBeamAbility(TractorBeamSettings settings)
        {
            _settings = settings;
            _movementPenalty = new CombatStatModifier(1f, 1f, settings.SpeedMultiplier, 1f, 1f);
        }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _caster = caster;
            _target = target;
            _range = definition.Range;
            HardPointType subsystem = _settings.RequiredSubsystem;
            _hardPoint = caster.Health.GetShipUnits(subsystem)
                .First(point => point.HardPointType == subsystem && !point.IsDestroyed);
            _targetTransform = target.GetFacade<IEntityTransformFacade>().Transform;
            _targetModifiers = target.GetFacade<ICombatModifiersFacade>().Modifiers;
            _view = Object.Instantiate(_settings.Beam.BeamPrefab);
            _view.PlayBeam(_hardPoint.Transform, _targetTransform, definition.Duration, _settings.Beam);
            _applied = _targetModifiers.Add(_movementPenalty);
        }

        public void Stop()
        {
            _targetModifiers.Remove(_applied);
            if (_view != null)
            {
                _view.StopBeam();
                Object.Destroy(_view.gameObject, _view.FadeDuration);
            }
        }
    }
}
