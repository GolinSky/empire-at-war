using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.Models.Health
{
    public sealed class HardPointAdapter : IHardPointModel, IDisposable
    {
        private readonly HardPointModel _model;
        private readonly IHardPoint _view;
        private bool _wasDestroyed;

        public event Action OnHardPointHealthChanged;
        public event Action OnDestroyed;

        public HardPointType HardPointType => _model.HardPointType;
        public float HealthPercentage => _model.HealthPercentage;
        public int Id => _model.Id;
        public int Generation => _model.Generation;
        public bool IsDestroyed => _model.IsDestroyed;
        public Vector3 Position => _view.Position;
        public Transform Transform => _view.Transform;

        public HardPointAdapter(HardPointModel model, IHardPoint view)
        {
            _model = model;
            _view = view;
            _wasDestroyed = _model.IsDestroyed;

            _model.OnHardPointHealthChanged += HandleHealthChanged;
            _view.UpdateData(_model.HealthPercentage);
        }

        public void Dispose()
        {
            _model.OnHardPointHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged()
        {
            bool wasDestroyed = _wasDestroyed;
            _wasDestroyed = _model.IsDestroyed;
            _view.UpdateData(_model.HealthPercentage);
            OnHardPointHealthChanged?.Invoke();
            if (!wasDestroyed && _wasDestroyed) OnDestroyed?.Invoke();
        }
    }
}
