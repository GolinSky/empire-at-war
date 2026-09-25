using System;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health
{
    /// <summary>Optional health subsystem coordinating the shield surface and its presentation.</summary>
    public sealed class ShieldComponent : IDisposable
    {
        private readonly IHealthModelObserver _health;
        private readonly IShieldView _view;
        private bool _active;

        public ShieldComponent(IHealthModelObserver health, IShieldView view)
        {
            _health = health;
            _view = view;
        }

        public void Initialize()
        {
            _health.OnValueChanged += UpdateState;
            UpdateState();
        }

        public Vector3 GetImpactPosition(Vector3 origin, Vector3 target)
        {
            return _active ? _view.GetSurfacePosition(origin, target) : target;
        }

        public void ShowImpact(Vector3 position)
        {
            if (_active) _view.ShowImpact(position);
        }

        public void Dispose()
        {
            _health.OnValueChanged -= UpdateState;
            _active = false;
            _view.SetActive(false);
        }

        private void UpdateState()
        {
            _active = _health.HasShields && !_health.IsLostShieldGenerator && !_health.IsDestroyed;
            _view.SetActive(_active);
        }
    }
}
