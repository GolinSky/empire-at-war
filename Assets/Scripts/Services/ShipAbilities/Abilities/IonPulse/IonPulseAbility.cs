using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class IonPulseAbility : IPhasedShipAbility, ITargetedShipAbility
    {
        private readonly IonPulseSettings _settings;
        private readonly IEntityLocator _entities;
        private readonly IPlayerRelations _relations;
        private readonly List<(CombatModifiers Modifiers, CombatModifierHandle Applied, float Remaining)> _stuns =
            new List<(CombatModifiers, CombatModifierHandle, float)>();
        private readonly HashSet<IEntity> _hits = new HashSet<IEntity>();
        private readonly CombatStatModifier _disable = new CombatStatModifier(1f, 1f, 0f, 1f, 1f);
        private IShipAbilityFacade _caster;
        private IAbilityFacingFacade _facing;
        private IHardPointModel _cannon;
        private IEntity _target;
        public IEntity Target => _target;
        private PlayerId _owner;
        private IonPulseModel _model;
        private IIonPulseView _view;
        private Vector3 _origin;
        private Vector3 _aimPoint;
        private Vector3 _direction;
        private float _rise;
        private float _range;
        private bool _cancelled;

        public bool IsComplete => _cancelled || (_model.HasFired &&
            _model.WaveComplete && _stuns.Count == 0);
        public bool SurvivesCasterDeath => _model.HasFired;

        public IonPulseAbility(IonPulseSettings settings, IEntityLocator entities, IPlayerRelations relations)
        {
            _settings = settings;
            _entities = entities;
            _relations = relations;
        }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _caster = caster;
            _owner = caster.Entity.Owner;
            _target = target;
            _aimPoint = target.GetFacade<IEntityTransformFacade>().Transform.position;
            _range = definition.Range;
            _model = new IonPulseModel(_settings.WaveSpeed);
            _facing = caster.Entity.GetFacade<IAbilityFacingFacade>();
            _facing.BeginAbilityFacing();
        }

        public void Advance(float deltaTime)
        {
            for (int i = _stuns.Count - 1; i >= 0; i--)
            {
                var stun = _stuns[i];
                float remaining = stun.Remaining - deltaTime;
                if (remaining > 0f)
                {
                    _stuns[i] = (stun.Modifiers, stun.Applied, remaining);
                    continue;
                }
                stun.Modifiers.Remove(stun.Applied);
                stun.Modifiers.RemoveIonPulseDisable();
                _stuns.RemoveAt(i);
            }
            _model.Advance(deltaTime);
            if (!_model.HasFired)
            {
                // A target lost mid-turn (destroyed or cloaked) keeps its last seen position as the aim point.
                if (!_target.HealthModel.IsDestroyed && !_target.IsCloaked())
                    _aimPoint = _target.GetFacade<IEntityTransformFacade>().Transform.position;
                if (_cannon == null || _cannon.IsDestroyed) _cannon = FindBestCannon();
                if (_cannon == null || !_facing.IsAbilityFacing || _caster.Modifiers.AbilitiesSuppressed)
                {
                    _cancelled = true;
                    return;
                }
                Transform ship = _caster.Entity.GetFacade<IEntityTransformFacade>().Transform;
                Vector3 targetPosition = _aimPoint;
                Vector3 bearing = targetPosition - ship.position;
                bearing.y = 0f;
                // The ion cannons sit on the flanks: turn that broadside toward the target.
                float side = CannonSide(ship, _cannon);
                _facing.FaceAbility(Quaternion.AngleAxis(-90f * side, Vector3.up) * bearing);
                if (Vector3.Angle(ship.right * side, bearing) > _settings.AlignmentTolerance) return;
                _origin = _cannon.Position;
                Vector3 toTarget = targetPosition - _origin;
                toTarget.y = 0f;
                _direction = toTarget.normalized;
                // Malevolence flies on the Deep tier; the wave climbs from the cannon to reach the target's height.
                _rise = (targetPosition.y - _origin.y) / Mathf.Max(toTarget.magnitude, 1f);
                // A station's center can lie past the ability range; the wave must still reach it.
                _model.Fire(Mathf.Max(_range, toTarget.magnitude + _settings.WaveThickness));
                _facing.EndAbilityFacing();
                _view = Object.Instantiate(_settings.ViewPrefab);
            }
            if (_view == null) return;
            Vector3 path = _direction + Vector3.up * _rise;
            _view.Show(_origin + path * _model.Distance, path.normalized, _settings.WaveRadius);
            foreach (IEntity entity in _entities.Entities)
            {
                if (_hits.Contains(entity) || entity.HealthModel.IsDestroyed ||
                    !_relations.IsHostile(_owner, entity.Owner) ||
                    entity.HealthModel.ShipClass.IsStrikecraft() ||
                    !entity.TryGetFacade(out ICombatModifiersFacade combat)) continue;
                Vector3 offset = entity.GetFacade<IEntityTransformFacade>().Transform.position - _origin;
                offset.y = 0f;
                float forward = Vector3.Dot(offset, _direction);
                float radial = (offset - _direction * forward).magnitude;
                if (!_model.Intersects(forward, radial, _settings.WaveRadius, _settings.WaveThickness)) continue;
                _hits.Add(entity);
                Disable(combat.Modifiers);
            }
            if (_model.WaveComplete)
            {
                _view.Release();
                _view = null;
            }
        }

        public void Stop()
        {
            _facing.EndAbilityFacing();
            if (_view != null)
            {
                _view.Release();
                _view = null;
            }
            foreach (var stun in _stuns)
            {
                stun.Modifiers.Remove(stun.Applied);
                stun.Modifiers.RemoveIonPulseDisable();
            }
            _stuns.Clear();
        }

        private void Disable(CombatModifiers modifiers)
        {
            CombatModifierHandle applied = modifiers.Add(_disable);
            modifiers.AddIonPulseDisable();
            _stuns.Add((modifiers, applied, _settings.DisableDuration));
        }

        // The live cannon whose broadside needs the smallest turn toward the target.
        private IHardPointModel FindBestCannon()
        {
            Transform ship = _caster.Entity.GetFacade<IEntityTransformFacade>().Transform;
            Vector3 bearing = _aimPoint - ship.position;
            bearing.y = 0f;
            IHardPointModel best = null;
            float bestAngle = float.MaxValue;
            foreach (IHardPointModel cannon in _caster.Health.GetShipUnits(HardPointType.IonPulseCannon))
            {
                // GetShipUnits falls back to every live hardpoint once no cannon is left, so the type is checked again.
                if (cannon.IsDestroyed || cannon.HardPointType != HardPointType.IonPulseCannon) continue;
                float angle = Vector3.Angle(ship.right * CannonSide(ship, cannon), bearing);
                if (angle >= bestAngle) continue;
                bestAngle = angle;
                best = cannon;
            }
            return best;
        }

        private static float CannonSide(Transform ship, IHardPointModel cannon) =>
            Mathf.Sign(ship.InverseTransformPoint(cannon.Position).x);
    }
}
