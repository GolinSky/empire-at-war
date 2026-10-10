using System;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health
{
    /// <summary>A whole-hull aim point for units without destructible subsystems.</summary>
    public sealed class HullTarget : IHardPointModel
    {
        private readonly HealthModel _health;
        private readonly Transform _transform;

        public event Action OnHardPointHealthChanged
        {
            add => _health.OnValueChanged += value;
            remove => _health.OnValueChanged -= value;
        }

        public event Action OnDestroyed
        {
            add => _health.OnDestroy += value;
            remove => _health.OnDestroy -= value;
        }

        public HardPointType HardPointType => HardPointType.Any;
        public float HealthPercentage => _health.HullPercentage;
        public int Id => 0;
        public int Generation => 0;
        public bool IsDestroyed => _health.IsDestroyed;
        public Vector3 Position => _transform.position;
        public Transform Transform => _transform;
        public Transform Pivot => _transform;

        public HullTarget(HealthModel health, Transform transform)
        {
            _health = health;
            _transform = transform;
        }
    }
}
