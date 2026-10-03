using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class IonPulseAbility : IPhasedShipAbility
    {
        private readonly IonPulseSettings _settings;
        private readonly IEntityLocator _entities;
        private readonly IPlayerRelations _relations;
        private readonly List<(CombatModifiers Modifiers, float Remaining)> _stuns = new List<(CombatModifiers, float)>();
        private readonly HashSet<IEntity> _hits = new HashSet<IEntity>();
        private readonly CombatStatModifier _disable = new CombatStatModifier(1f, 1f, 0f, 1f, 1f);
        private IShipAbilityFacade _caster;
        private IAbilityFacingFacade _facing;
        private IHardPointModel _cannon;
        private IEntity _target;
        private PlayerId _owner;
        private IonPulseModel _model;
        private IIonPulseView _view;
        private Vector3 _origin;
        private Vector3 _direction;
        private float _side;
        private float _range;
        private bool _cancelled;
        private bool _malfunctioned;

        public bool IsComplete => _cancelled || (_model.HasFired &&
            (_malfunctioned || _model.WaveComplete) && _stuns.Count == 0);
        public bool SurvivesCasterDeath => _model.HasFired;

        public IonPulseAbility(IonPulseSettings settings, IEntityLocator entities, IPlayerRelations relations)
        {
            _settings = settings;
            _entities = entities;
            _relations = relations;
        }

        public bool CanStart(IShipAbilityFacade caster, IEntity target)
        {
            if (target.HealthModel.ShipClass == ShipClass.Fighter || target.HealthModel.ShipClass == ShipClass.Bomber)
                return false;
            foreach (IHardPointModel cannon in caster.Health.GetShipUnits(HardPointType.IonPulseCannon))
                if (!cannon.IsDestroyed) return true;
            return false;
        }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _caster = caster;
            _owner = caster.Entity.Owner;
            _target = target;
            _range = definition.Range;
            _model = new IonPulseModel(_settings.ChargeDuration, _settings.AlignmentTimeout, _settings.WaveSpeed, _range);
            Transform ship = caster.Entity.GetFacade<IEntityTransformFacade>().Transform;
            Vector3 targetPosition = target.GetFacade<IEntityTransformFacade>().Transform.position;
            float bestAngle = float.MaxValue;
            foreach (IHardPointModel cannon in caster.Health.GetShipUnits(HardPointType.IonPulseCannon))
            {
                if (cannon.IsDestroyed) continue;
                float side = Mathf.Sign(ship.InverseTransformPoint(cannon.Position).x);
                float angle = Vector3.Angle(ship.right * side, targetPosition - ship.position);
                if (angle >= bestAngle) continue;
                bestAngle = angle;
                _cannon = cannon;
                _side = side;
            }
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
                    _stuns[i] = (stun.Modifiers, remaining);
                    continue;
                }
                stun.Modifiers.Remove(_disable);
                stun.Modifiers.RemoveIonPulseDisable();
                _stuns.RemoveAt(i);
            }
            _model.Advance(deltaTime);
            if (!_model.HasFired)
            {
                if (_target.HealthModel.IsDestroyed || _cannon.IsDestroyed || !_facing.IsAbilityFacing ||
                    _caster.Modifiers.IsIonDisabled || _model.AlignmentExpired)
                {
                    _cancelled = true;
                    return;
                }
                if (!_model.IsCharged) return;
                Transform ship = _caster.Entity.GetFacade<IEntityTransformFacade>().Transform;
                Vector3 toTarget = _target.GetFacade<IEntityTransformFacade>().Transform.position - _cannon.Position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > _range * _range)
                {
                    _cancelled = true;
                    return;
                }
                _direction = toTarget.normalized;
                _facing.FaceAbility(Quaternion.AngleAxis(-90f * _side, Vector3.up) * _direction);
                if (Vector3.Angle(ship.right * _side, _direction) > _settings.AlignmentTolerance) return;
                _origin = _cannon.Position;
                _model.Fire();
                _facing.EndAbilityFacing();
                _malfunctioned = Random.value < _settings.MalfunctionChance;
                if (_malfunctioned)
                {
                    Disable(_caster.Modifiers);
                    return;
                }
                _view = Object.Instantiate(_settings.ViewPrefab);
            }
            if (_malfunctioned || _view == null) return;
            _view.Show(_origin + _direction * _model.Distance, _direction, _settings.WaveRadius);
            foreach (IEntity entity in _entities.Entities)
            {
                if (_hits.Contains(entity) || entity.HealthModel.IsDestroyed ||
                    !_relations.IsHostile(_owner, entity.Owner) ||
                    entity.HealthModel.ShipClass == ShipClass.Fighter || entity.HealthModel.ShipClass == ShipClass.Bomber ||
                    !entity.TryGetFacade(out ICombatModifiersFacade combat)) continue;
                Vector3 offset = entity.GetFacade<IEntityTransformFacade>().Transform.position - _origin;
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
                stun.Modifiers.Remove(_disable);
                stun.Modifiers.RemoveIonPulseDisable();
            }
            _stuns.Clear();
        }

        private void Disable(CombatModifiers modifiers)
        {
            modifiers.Add(_disable);
            modifiers.AddIonPulseDisable();
            _stuns.Add((modifiers, _settings.DisableDuration));
        }
    }
}
