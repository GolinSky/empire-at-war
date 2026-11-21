using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Extensions;
using EmpireAtWar.Models.Health;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Services.Repair
{
    public sealed class RepairSourceFacade : IRepairSourceFacade
    {
        private readonly HealthModel _health;
        private readonly CombatModifiers _modifiers;
        private readonly Transform _transform;
        private readonly List<IEntity> _targets = new List<IEntity>();

        public event Action TargetsChanged;

        public RepairSourceFacade(RepairProfile profile, HealthModel health, CombatModifiers modifiers,
            [Key(EntityBindType.ViewTransform)] Transform transform)
        {
            Profile = profile;
            _health = health;
            _modifiers = modifiers;
            _transform = transform;
        }

        public RepairProfile Profile { get; }
        public Vector3 Position => _transform.position;
        public bool CanRepair => !_health.IsDestroyed && !_modifiers.IsConstructing && !_modifiers.IsIonDisabled;
        public IReadOnlyList<IEntity> Targets => _targets;

        public void SetTargets(IReadOnlyList<IEntity> targets)
        {
            _targets.Clear();
            for (int i = 0; i < targets.Count; i++) _targets.Add(targets[i]);
            TargetsChanged?.Invoke();
        }
    }
}
