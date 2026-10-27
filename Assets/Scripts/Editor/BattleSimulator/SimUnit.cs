using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class SimUnit
    {
        // Ships and squadrons never upgrade at runtime (only stations do), so higher-level hardpoints stay uninstalled.
        private const int UNIT_LEVEL = 1;
        private readonly HealthModel _ship;
        private readonly SquadronHealthModel _squadron;
        private double _nextRegen;
        public SimUnitDefinition Definition { get; }
        public CombatModifiers Modifiers { get; } = new CombatModifiers();
        public HardPointModel[] HardPoints { get; }
        public readonly List<SimMount> Mounts = new List<SimMount>();
        // Live mounts beyond this count are out of arc; they take over when bearing mounts are destroyed.
        public int Bearing { get; }
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
                HardPoints[i] = new HardPointModel(definition.HardPoints[i].Type, i,
                    definition.HardPoints[i].UnlockLevel, definition.HardPoints[i].HealthOverride);
            if (definition.IsSquadron)
            {
                _squadron = new SquadronHealthModel(definition, matrix, Modifiers);
                _squadron.InitializeMembers(HardPoints);
            }
            else
            {
                _ship = new HealthModel(definition, matrix, Modifiers);
                _ship.InitializeHardPoints(HardPoints);
            }
            Stats = new UnitCombatStats { Name = definition.Name, UnitId = definition.Id, Side = side,
                Index = index, MaxHull = Hull, MaxShields = Shields };
            foreach (SimMountDefinition mount in definition.Mounts)
            {
                if (mount.UnlockLevel > UNIT_LEVEL || !HardPoints[mount.HardPointId].IsInstalled) continue;
                Stats.BaseDps += mount.Profile.DamagePerSecond;
                Mounts.Add(new SimMount { Definition = mount, SalvoStart = alphaStrike ? 0 : random.NextDouble() * mount.Profile.Reload,
                    Hits = new HitAccumulator((float)random.NextDouble()) });
            }
            Bearing = (int)Math.Round(Mounts.Count * entry.ArcFactor, MidpointRounding.AwayFromZero);
            // A seeded shuffle picks which mounts bear, so a partial arc does not depend on prefab order.
            if (Bearing < Mounts.Count)
                for (int i = Mounts.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (Mounts[i], Mounts[j]) = (Mounts[j], Mounts[i]);
                }
            UpdateStats();
        }

        public bool HasLiveHardPoints => Definition.IsSquadron ? _squadron.HasLiveHardPoints : _ship.HasLiveHardPoints;

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
