using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class SimUnit
    {
        private readonly HealthModel _ship;
        private readonly SquadronHealthModel _squadron;
        private double _nextRegen;
        public SimUnitDefinition Definition { get; }
        public CombatModifiers Modifiers { get; } = new CombatModifiers();
        public HardPointModel[] HardPoints { get; }
        public readonly List<SimMount> Mounts = new List<SimMount>();
        public UnitCombatStats Stats { get; }
        public int TargetCursor;
        public bool IsDestroyed => Definition.IsSquadron ? _squadron.IsDestroyed : _ship.IsDestroyed;
        public float Hull => Definition.IsSquadron ? _squadron.Hull : _ship.Hull;
        public float Shields => Definition.IsSquadron ? _squadron.Shields : _ship.Shields;
        public bool LostGenerator => !Definition.IsSquadron && _ship.IsLostShieldGenerator;

        public SimUnit(SimUnitDefinition definition, BattleFleetEntry entry, int side, int index,
            DamageMatrixData matrix, Random random, bool alphaStrike)
        {
            Definition = definition;
            HardPoints = new HardPointModel[definition.HardPoints.Count];
            for (int i = 0; i < HardPoints.Length; i++)
                HardPoints[i] = new HardPointModel(definition.HardPoints[i].Type, i, definition.HardPoints[i].UnlockLevel);
            if (definition.IsSquadron)
            {
                _squadron = new SquadronHealthModel(definition, matrix, Modifiers);
                _squadron.InitializeMembers(HardPoints);
            }
            else
            {
                _ship = new HealthModel(definition, matrix, Modifiers);
                _ship.InitializeHardPoints(HardPoints);
                _ship.Upgrade(entry.Level, 1, 1, 1);
            }
            Stats = new UnitCombatStats { Name = definition.Name, UnitId = definition.Id, Side = side,
                Index = index, MaxHull = Hull, MaxShields = Shields };
            int available = 0;
            foreach (SimMountDefinition mount in definition.Mounts)
                if (mount.UnlockLevel <= entry.Level && HardPoints[mount.HardPointId].IsInstalled) available++;
            int bearing = (int)Math.Round(available * entry.ArcFactor, MidpointRounding.AwayFromZero);
            foreach (SimMountDefinition mount in definition.Mounts)
            {
                if (mount.UnlockLevel > entry.Level || !HardPoints[mount.HardPointId].IsInstalled) continue;
                Stats.BaseDps += mount.Profile.DamagePerSecond;
                if (Mounts.Count >= bearing) continue;
                Mounts.Add(new SimMount { Definition = mount, SalvoStart = alphaStrike ? 0 : random.NextDouble() * mount.Profile.Reload });
            }
            UpdateStats();
        }

        public void ApplyDamage(float damage, DamageType type, int point)
        {
            if (Definition.IsSquadron) _squadron.ApplyDamage(damage, type, point);
            else _ship.ApplyDamage(damage, type, point);
        }

        public void Regenerate(double time, float step)
        {
            if (IsDestroyed) return;
            if (Definition.IsSquadron) _squadron.RepairHull(Definition.HullRepairPerSecond * step);
            if (time + 0.000001 < _nextRegen || LostGenerator) return;
            // Runtime timers start complete. Ship timers restart only when shields need regeneration.
            if (Definition.IsSquadron)
                _squadron.RegenerateShields(Definition.ShieldRegenerateValue * Modifiers.ShieldRegenMultiplier);
            else
            {
                if (_ship.Shields >= _ship.MaxShields) return;
                _ship.RegenerateShields(_ship.ShieldRegenerateValue * Modifiers.ShieldRegenMultiplier);
            }
            _nextRegen = time + Definition.ShieldRegenerateDelay;
        }

        public void UpdateStats() { Stats.Hull = Hull; Stats.Shields = Shields; }
    }
}
