using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Ship;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Stable target-specific strike membership inside one geographic task group.</summary>
    public sealed class EnemyAttackTaskForce
    {
        private const float SCREEN_CLEAR_SECONDS = 30f;
        private readonly EnemyTaskForceSelectionModel _selection = new EnemyTaskForceSelectionModel();
        private readonly List<ForceComposition> _candidates = new List<ForceComposition>();
        private readonly List<int> _retained = new List<int>();
        private readonly List<int> _selected = new List<int>();
        private readonly ForceComposition _bodyForce = new ForceComposition();
        private readonly ForceComposition _screenForce = new ForceComposition();
        private readonly HashSet<IShipEntity> _screens = new HashSet<IShipEntity>();
        private IEntity _target;
        private StructureKind? _targetKind;

        public List<IShipEntity> Ships { get; } = new List<IShipEntity>();

        public void Clear()
        {
            _target = null;
            _targetKind = null;
            Ships.Clear();
        }

        public bool Assemble(EnemyStrategicContext context, IEntity target, int maximumCount)
        {
            if (_target != target)
            {
                Clear();
                _target = target;
                if (target.TryGetFacade(out IStructureKindFacade structure)) _targetKind = structure.Kind;
            }
            _candidates.Clear();
            _retained.Clear();
            for (int i = 0; i < context.Ships.Count; i++)
            {
                IShipEntity ship = context.Ships[i];
                _candidates.Add(context.ShipForces[ship]);
                if (Ships.Contains(ship)) _retained.Add(i);
            }
            var health = target.HealthModel;
            bool ready = _selection.Select(_candidates, context.TargetForces[target], health.ShipClass,
                health.Hull, health.Shields, EnemyAiDifficultyProfile.Get(context.Snapshot.Difficulty).RequiredAttackRatio,
                _retained, maximumCount, _selected, _targetKind);
            Ships.Clear();
            foreach (int index in _selected) Ships.Add(context.Ships[index]);
            return ready;
        }

        public bool IsReady(EnemyStrategicContext context, IReadOnlyList<IShipEntity> body)
        {
            _bodyForce.Clear();
            foreach (IShipEntity ship in body) _bodyForce.Add(context.ShipForces[ship]);
            var health = _target.HealthModel;
            return EnemyTaskForceSelectionModel.IsReady(_bodyForce, context.TargetForces[_target],
                health.ShipClass, health.Hull, health.Shields,
                EnemyAiDifficultyProfile.Get(context.Snapshot.Difficulty).RequiredAttackRatio, _targetKind);
        }

        public float TargetDps(EnemyStrategicContext context, IShipEntity ship)
        {
            var health = _target.HealthModel;
            return EffectiveDps(context.ShipForces[ship], health.ShipClass, health.Hull, health.Shields, _targetKind);
        }

        public void AssignScreens(EnemyStrategicContext context, IReadOnlyList<IShipEntity> body, IShipEntity strikeAnchor)
        {
            _screens.Clear();
            _screenForce.Clear();
            ForceComposition defenders = context.TargetForces[_target];
            while (CombatMatchup.TimeToDestroy(_screenForce, defenders) > SCREEN_CLEAR_SECONDS)
            {
                IShipEntity best = null;
                float bestEfficiency = 1f;
                foreach (IShipEntity ship in body)
                {
                    if (ship == strikeAnchor || _screens.Contains(ship)) continue;
                    float targetDps = TargetDps(context, ship);
                    foreach (var shipClass in ForceComposition.Classes)
                    {
                        if (defenders.Hull(shipClass) <= 0f) continue;
                        float defenderDps = EffectiveDps(context.ShipForces[ship], shipClass,
                            defenders.Hull(shipClass), defenders.Shields(shipClass));
                        float efficiency = defenderDps / System.Math.Max(targetDps, float.Epsilon);
                        if (efficiency <= bestEfficiency) continue;
                        best = ship;
                        bestEfficiency = efficiency;
                    }
                }
                if (best == null) break;
                _screens.Add(best);
                _screenForce.Add(context.ShipForces[best]);
            }
            // Dedicated counters cannot attack the primary target, but can still engage its defenders.
            foreach (IShipEntity ship in body)
                if (ship != strikeAnchor && TargetDps(context, ship) == 0f) _screens.Add(ship);
        }

        public bool IsScreen(IShipEntity ship) => _screens.Contains(ship);

        private static float EffectiveDps(ForceComposition force,
            ShipClass shipClass, float hull, float shields, StructureKind? targetKind = null)
        {
            float time = CombatMatchup.TimeToDestroyTarget(force, shipClass, hull, shields, targetKind);
            return time >= CombatMatchup.MAX_TIME ? 0f : (hull + shields) / System.Math.Max(time, float.Epsilon);
        }
    }
}
