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
    public sealed class TractorBeamAbility : IPhasedShipAbility, ITargetedShipAbility, IReplicaPersistentShipAbility
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
        public bool SurvivesCasterDeath => false;

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
            _hardPoint = caster.Health.GetShipUnits(HardPointType.TractorBeam)
                .First(point => point.HardPointType == HardPointType.TractorBeam && !point.IsDestroyed);
            _targetTransform = target.GetFacade<IEntityTransformFacade>().Transform;
            _targetModifiers = target.GetFacade<ICombatModifiersFacade>().Modifiers;
            // Resolve the beam prefab first: a wrong prefab must fail before the target is slowed.
            BeamShot beamPrefab = (BeamShot)_settings.Beam.ShotPrefab;
            _applied = _targetModifiers.Add(_movementPenalty);
            _view = Object.Instantiate(beamPrefab);
            _view.PlayBeam(_hardPoint.Transform, _targetTransform, definition.Duration, _settings.Beam);
        }

        public void Advance(float deltaTime) { }

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
