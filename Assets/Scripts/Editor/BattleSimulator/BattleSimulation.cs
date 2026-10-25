using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class BattleSimulation
    {
        public const double STEP_SECONDS = 0.05;
        private readonly BattleSetup _setup;
        private readonly Random _random;
        private readonly WeaponModel _weapons;
        private readonly bool _detailed;
        private readonly List<SimUnit> _units = new List<SimUnit>();
        private readonly List<SimUnit> _candidates = new List<SimUnit>();
        private readonly List<int> _points = new List<int>();
        private readonly SortedDictionary<int, List<SimImpact>> _impacts = new SortedDictionary<int, List<SimImpact>>();
        private readonly ITargetPolicy[] _policies;
        private int _tick;
        public BattleResult Result { get; }
        public bool IsComplete { get; private set; }

        public BattleSimulation(BattleSetup setup, IReadOnlyDictionary<string, SimUnitDefinition> definitions,
            DamageMatrixData matrix, int seed, bool detailed = true)
        {
            _setup = setup;
            _random = new Random(seed);
            _weapons = new WeaponModel(matrix.CopyAccuracy());
            _detailed = detailed;
            Result = new BattleResult { Seed = seed };
            _policies = new[] { Policy(setup.PolicyA), Policy(setup.PolicyB) };
            if (!float.IsFinite(setup.TimeLimit) || setup.TimeLimit <= 0 || !float.IsFinite(setup.Distance) || setup.Distance < -1)
                throw new InvalidOperationException("Time limit must be positive; distance must be -1 (unit range) or nonnegative.");
            AddSide(setup.SideA, 0, definitions, matrix);
            AddSide(setup.SideB, 1, definitions, matrix);
            Sample(0);
        }

        private void AddSide(List<BattleFleetEntry> entries, int side, IReadOnlyDictionary<string, SimUnitDefinition> definitions, DamageMatrixData matrix)
        {
            if (entries.Count == 0) throw new InvalidOperationException("Both sides need at least one unit.");
            foreach (BattleFleetEntry entry in entries)
            {
                if (entry.Quantity < 1 || entry.Quantity > 100 || !float.IsFinite(entry.ArcFactor) || entry.ArcFactor < 0 || entry.ArcFactor > 1)
                    throw new InvalidOperationException("Quantity must be 1–100 and arc factor 0–1.");
                for (int i = 0; i < entry.Quantity; i++)
                {
                    var unit = new SimUnit(definitions[entry.UnitId], entry, side, _units.Count, matrix, _random, _setup.AlphaStrike);
                    _units.Add(unit);
                    Result.Units.Add(unit.Stats);
                }
            }
        }

        public void Step()
        {
            if (IsComplete) return;
            double time = _tick * STEP_SECONDS;
            foreach (SimUnit unit in _units)
            {
                if (unit.IsDestroyed || unit.Modifiers.IsIonDisabled) continue;
                int bearing = 0;
                foreach (SimMount mount in unit.Mounts)
                {
                    if (unit.HardPoints[mount.Definition.HardPointId].IsDestroyed) continue;
                    // Out-of-arc mounts keep their cycle without firing, so they take over a destroyed mount's place in step.
                    bool fires = bearing++ < unit.Bearing;
                    while (mount.NextShot <= time + 0.000001)
                    {
                        if (fires) Fire(unit, mount, time);
                        mount.Advance();
                    }
                }
            }
            if (_impacts.TryGetValue(_tick, out List<SimImpact> impacts))
            {
                foreach (SimImpact impact in impacts) BattleDamageResolver.Apply(impact, time, Result, _detailed);
                _impacts.Remove(_tick);
            }
            bool aliveA = false, aliveB = false;
            foreach (SimUnit unit in _units)
            {
                unit.Regenerate(time, _tick == 0 ? 0 : (float)STEP_SECONDS);
                unit.UpdateStats();
                if (!unit.IsDestroyed) { if (unit.Stats.Side == 0) aliveA = true; else aliveB = true; }
            }
            Result.Duration = time;
            IsComplete = !aliveA || !aliveB || time + STEP_SECONDS > _setup.TimeLimit + 0.000001;
            if (IsComplete)
            {
                Result.Winner = aliveA == aliveB ? -1 : aliveA ? 0 : 1;
                Result.Reason = !aliveA && !aliveB ? "Mutual destruction" : aliveA && aliveB ? "Timeout" : "Elimination";
            }
            if (_tick % 10 == 0 || IsComplete) Sample(time);
            _tick++;
        }

        private void Fire(SimUnit source, SimMount mount, double time)
        {
            WeaponProfile profile = mount.Definition.Profile;
            _candidates.Clear();
            foreach (SimUnit target in _units)
                if (!target.IsDestroyed && target.Stats.Side != source.Stats.Side &&
                    (!profile.StrikecraftOnly || target.Definition.ShipClass.IsStrikecraft())) _candidates.Add(target);
            if (_candidates.Count == 0) return;
            SimUnit selected = _policies[source.Stats.Side].Select(source, _candidates);
            int point = SelectPoint(selected);
            source.Stats.Shots++;
            if (!mount.Hits.Roll(_weapons.GetHitChance(profile.DamageType, selected.Definition.ShipClass,
                    profile.GetAccuracyBias(selected.Definition.ShipClass) * source.Modifiers.AccuracyMultiplier),
                    (float)_random.NextDouble())) return;
            source.Stats.Hits++;
            float distance = _setup.Distance < 0 ? source.Definition.Range : _setup.Distance;
            double delay = mount.Definition.HasFixedFlight ? mount.Definition.FixedFlightSeconds : distance / profile.ProjectileSpeed;
            int ticks = Math.Max(1, (int)Math.Ceiling(mount.Definition.DamageDuration / WeaponComponent.DAMAGE_TICK_INTERVAL));
            for (int i = 0; i < ticks; i++)
            {
                double arrival = time + delay + mount.Definition.DamageDuration * i / ticks;
                int tick = Math.Max(_tick, (int)Math.Ceiling(arrival / STEP_SECONDS - 0.000001));
                if (!_impacts.TryGetValue(tick, out List<SimImpact> queue)) _impacts.Add(tick, queue = new List<SimImpact>());
                queue.Add(new SimImpact { Time = arrival, Source = source, Target = selected, HardPointId = point,
                    DamageType = profile.DamageType, Damage = profile.Damage * profile.GetDamageBias(selected.Definition.ShipClass)
                        * source.Modifiers.DamageMultiplier / ticks });
            }
        }

        private int SelectPoint(SimUnit target)
        {
            _points.Clear();
            foreach (var point in target.HardPoints)
                if (!point.IsDestroyed && (_setup.HardPointFocus == HardPointType.Any || point.HardPointType == _setup.HardPointFocus)) _points.Add(point.Id);
            if (_points.Count == 0)
                foreach (var point in target.HardPoints) if (!point.IsDestroyed) _points.Add(point.Id);
            // Runtime still routes hull damage through an installed, destroyed hardpoint when all are lost.
            if (_points.Count == 0)
                foreach (var point in target.HardPoints) if (point.IsInstalled) _points.Add(point.Id);
            return _points.Count == 0 ? 0 : _points[_random.Next(_points.Count)];
        }

        private void Sample(double time)
        {
            if (!_detailed) return;
            var sample = new BattleSample { Time = time };
            foreach (SimUnit unit in _units)
                if (!unit.IsDestroyed)
                {
                    if (unit.Stats.Side == 0) { sample.HullA += unit.Hull; sample.ShieldsA += unit.Shields; }
                    else { sample.HullB += unit.Hull; sample.ShieldsB += unit.Shields; }
                }
            Result.Timeline.Add(sample);
        }

        private static ITargetPolicy Policy(BattleTargetPolicy policy) => policy switch
        {
            BattleTargetPolicy.FocusWeakest => new WeakestTargetPolicy(),
            BattleTargetPolicy.FocusStrongest => new StrongestTargetPolicy(),
            BattleTargetPolicy.Spread => new SpreadTargetPolicy(),
            BattleTargetPolicy.SameClassFirst => new SameClassTargetPolicy(),
            _ => throw new ArgumentOutOfRangeException(nameof(policy))
        };
    }
}
