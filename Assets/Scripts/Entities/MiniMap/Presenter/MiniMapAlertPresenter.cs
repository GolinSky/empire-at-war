using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Presenters.MiniMap
{
    /// <summary>
    /// Pings where local units take damage, including replicated health changes on online clients.
    /// Combat pings are limited to one per map cell per <see cref="COMBAT_COOLDOWN_SECONDS"/>.
    /// </summary>
    [LifecycleOrder(Initialize = LifecycleOrders.HUD_PRESENTERS_INITIALIZE)]
    public sealed class MiniMapAlertPresenter : IInitializable, ILateDisposable, IObserver<BattleState>
    {
        private const float COMBAT_COOLDOWN_SECONDS = 4f;
        private const int COMBAT_CELLS_PER_SIDE = 10;

        private readonly MiniMapData _miniMapData;
        private readonly IMapModelObserver _map;
        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _localPlayer;
        private readonly INotifier<BattleState> _battleState;
        private readonly Dictionary<long, HealthWatch> _watches = new Dictionary<long, HealthWatch>();
        private readonly Dictionary<int, float> _combatCellReadyAt = new Dictionary<int, float>();
        private bool _running;

        public MiniMapAlertPresenter(MiniMapData miniMapData, IMapModelObserver map,
            IEntityLocator entities, ILocalPlayer localPlayer,
            INotifier<BattleState> battleState)
        {
            _miniMapData = miniMapData;
            _map = map;
            _entities = entities;
            _localPlayer = localPlayer;
            _battleState = battleState;
        }

        public void Initialize() => _battleState.AddObserver(this);

        // Existing health is the initial baseline, so loading never pings.
        public void UpdateState(BattleState state)
        {
            if (state != BattleState.Running || _running) return;
            _running = true;
            _entities.EntityAdded += HandleEntityAdded;
            _entities.EntityRemoved += HandleEntityRemoved;
            foreach (IEntity entity in _entities.Entities) HandleEntityAdded(entity);
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            if (!_running) return;
            _running = false;
            _entities.EntityAdded -= HandleEntityAdded;
            _entities.EntityRemoved -= HandleEntityRemoved;
            foreach (HealthWatch watch in _watches.Values) watch.Dispose();
            _watches.Clear();
        }

        private void HandleEntityAdded(IEntity entity)
        {
            if (!_localPlayer.IsLocal(entity.Owner) || _watches.ContainsKey(entity.Id) ||
                !entity.TryGetFacade(out IEntityTransformFacade transform)) return;
            _watches.Add(entity.Id, new HealthWatch(entity.HealthModel, transform.Transform, HandleDamaged));
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            if (!_watches.Remove(entity.Id, out HealthWatch watch)) return;
            watch.Dispose();
        }

        private void HandleDamaged(Vector3 position)
        {
            int cell = GetCombatCell(position);
            float now = Time.unscaledTime;
            if (_combatCellReadyAt.TryGetValue(cell, out float readyAt) && now < readyAt) return;
            _combatCellReadyAt[cell] = now + COMBAT_COOLDOWN_SECONDS;
            _miniMapData.AddAlert(MiniMapAlertType.Combat, position);
        }

        private int GetCombatCell(Vector3 position)
        {
            Vector2Range range = _map.SizeRange;
            int x = Mathf.Clamp((int)(Mathf.InverseLerp(range.Min.x, range.Max.x, position.x) * COMBAT_CELLS_PER_SIDE),
                0, COMBAT_CELLS_PER_SIDE - 1);
            int y = Mathf.Clamp((int)(Mathf.InverseLerp(range.Min.y, range.Max.y, position.z) * COMBAT_CELLS_PER_SIDE),
                0, COMBAT_CELLS_PER_SIDE - 1);
            return y * COMBAT_CELLS_PER_SIDE + x;
        }

        /// <summary>Reports a hull or shield drop; online clients see it through replicated health.</summary>
        private sealed class HealthWatch
        {
            private readonly IHealthModelObserver _health;
            private readonly Transform _transform;
            private readonly System.Action<Vector3> _damaged;
            private float _total;

            public HealthWatch(IHealthModelObserver health, Transform transform, System.Action<Vector3> damaged)
            {
                _health = health;
                _transform = transform;
                _damaged = damaged;
                _total = Total;
                _health.OnValueChanged += HandleValueChanged;
            }

            private float Total => _health.Hull + _health.Shields;

            private void HandleValueChanged()
            {
                float total = Total;
                if (total < _total) _damaged(_transform.position);
                _total = total;
            }

            public void Dispose() => _health.OnValueChanged -= HandleValueChanged;
        }
    }
}
