using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Reports when a unit owned by the local player loses hull or shields; drives the hint combat pause.</summary>
    public sealed class LocalUnitDamageTracker : ILateDisposable
    {
        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _player;
        private readonly Dictionary<long, HealthWatch> _watches = new Dictionary<long, HealthWatch>();

        public event Action LocalUnitDamaged;

        public LocalUnitDamageTracker(IEntityLocator entities, ILocalPlayer player)
        {
            _entities = entities;
            _player = player;
            _entities.EntityAdded += HandleAdded;
            _entities.EntityRemoved += HandleRemoved;
            foreach (IEntity entity in _entities.Entities) HandleAdded(entity);
        }

        private void HandleAdded(IEntity entity)
        {
            if (!_player.IsLocal(entity.Owner) || _watches.ContainsKey(entity.Id)) return;
            _watches.Add(entity.Id, new HealthWatch(entity.HealthModel, NotifyDamaged));
        }

        private void HandleRemoved(IEntity entity)
        {
            if (!_watches.TryGetValue(entity.Id, out HealthWatch watch)) return;
            watch.Dispose();
            _watches.Remove(entity.Id);
        }

        private void NotifyDamaged() => LocalUnitDamaged?.Invoke();

        public void LateDispose()
        {
            _entities.EntityAdded -= HandleAdded;
            _entities.EntityRemoved -= HandleRemoved;
            foreach (HealthWatch watch in _watches.Values) watch.Dispose();
            _watches.Clear();
        }

        private sealed class HealthWatch : IDisposable
        {
            private readonly IHealthModelObserver _health;
            private readonly Action _damaged;
            private float _total;

            public HealthWatch(IHealthModelObserver health, Action damaged)
            {
                _health = health;
                _damaged = damaged;
                _total = Total;
                _health.OnValueChanged += HandleValueChanged;
            }

            private float Total => _health.Hull + _health.Shields;

            private void HandleValueChanged()
            {
                float total = Total;
                if (total < _total) _damaged();
                _total = total;
            }

            public void Dispose() => _health.OnValueChanged -= HandleValueChanged;
        }
    }
}
