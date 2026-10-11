using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Components.Weapon
{
    // Shared only during the capture boundary; commits still validate live targets.
    internal sealed class TargetSelectionCapture
    {
        private readonly Dictionary<IHealthModelObserver, TargetState> _targets =
            new Dictionary<IHealthModelObserver, TargetState>();
        private readonly Dictionary<IHardPointModel, WeaponComponent.TargetSelectionCandidate> _units =
            new Dictionary<IHardPointModel, WeaponComponent.TargetSelectionCandidate>();

        public void Clear()
        {
            _targets.Clear();
            _units.Clear();
        }

        public bool IsWreck(AttackData group)
        {
            TargetState state = GetTarget(group.TargetHealth);
            return !state.IsDestroyed && !state.HasLiveHardPoints;
        }

        public bool TryCapture(AttackData group, IHardPointModel unit,
            out WeaponComponent.TargetSelectionCandidate candidate)
        {
            TargetState state = GetTarget(group.TargetHealth);
            if (state.IsDestroyed || unit.IsDestroyed && state.HasLiveHardPoints)
            {
                candidate = default;
                return false;
            }

            if (!_units.TryGetValue(unit, out candidate))
            {
                candidate = new WeaponComponent.TargetSelectionCandidate
                {
                    Unit = unit,
                    Generation = unit.Generation,
                    Position = unit.Position,
                    IsStrikecraft = state.IsStrikecraft
                };
                _units.Add(unit, candidate);
                AttackSequenceDiagnostics.RecordTargetSnapshot();
            }
            // Each owner may have its own AttackData wrapper for this same hardpoint.
            candidate.Group = group;
            return true;
        }

        private TargetState GetTarget(IHealthModelObserver health)
        {
            if (_targets.TryGetValue(health, out TargetState state)) return state;
            state = new TargetState
            {
                IsDestroyed = health.IsDestroyed,
                HasLiveHardPoints = !health.IsDestroyed && health.HasLiveHardPoints,
                IsStrikecraft = health.ShipClass.IsStrikecraft()
            };
            _targets.Add(health, state);
            return state;
        }

        private struct TargetState
        {
            public bool IsDestroyed;
            public bool HasLiveHardPoints;
            public bool IsStrikecraft;
        }
    }
}
