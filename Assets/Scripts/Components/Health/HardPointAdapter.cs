using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.Models.Health
{
    public sealed class HardPointAdapter : IHardPointModel, IDisposable
    {
        private readonly HardPointModel _model;
        private readonly IHardPointView _view;

        public event Action OnHardPointHealthChanged;

        public HardPointType HardPointType => _model.HardPointType;
        public float HealthPercentage => _model.HealthPercentage;
        public int Id => _model.Id;
        public int Generation => _model.Generation;
        public bool IsDestroyed => _model.IsDestroyed;
        public Vector3 Position => _view.Position;
        public Transform Transform => _view.Transform;

        public HardPointAdapter(HardPointModel model, IHardPointView view)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _view = view ?? throw new ArgumentNullException(nameof(view));

            _model.OnHardPointHealthChanged += HandleHealthChanged;
            _view.UpdateData(_model.HealthPercentage);
        }

        public void Dispose()
        {
            _model.OnHardPointHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged()
        {
            _view.UpdateData(_model.HealthPercentage);
            OnHardPointHealthChanged?.Invoke();
        }
    }
}
