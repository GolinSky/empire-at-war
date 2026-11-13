using System;
using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public static class BattleDamageResolver
    {
        public static void Apply(SimImpact impact, double time, BattleResult result, bool detailed)
        {
            SimUnit target = impact.Target;
            var point = target.HardPoints.Length == 0 ? null : target.HardPoints[impact.HardPointId];
            // Runtime CanStillHit preserves impacts on dead ship mounts, but not dead strikecraft members.
            if (target.IsDestroyed || (point != null && point.IsDestroyed && target.Definition.ShipClass.IsStrikecraft()))
            {
                impact.Source.Stats.Overkill += impact.Damage;
                return;
            }
            float hull = target.Hull;
            float shields = target.Shields;
            Span<float> pointHealth = stackalloc float[target.HardPoints.Length];
            for (int i = 0; i < pointHealth.Length; i++) pointHealth[i] = target.HardPoints[i].Health;
            bool lostGenerator = target.LostGenerator;
            target.ApplyDamage(impact.Damage, impact.DamageType, impact.HardPointId);
            double hullDamage = hull - target.Hull;
            double shieldDamage = shields - target.Shields;
            target.Stats.HullDamage += hullDamage;
            target.Stats.ShieldDamage += shieldDamage;
            impact.Source.Stats.DamageDealt += hullDamage + shieldDamage;
            if (shields > 0 && target.Shields == 0) Add("Shields down", "", target, time, result, detailed);
            if (!lostGenerator && target.LostGenerator) Add("Shield generator lost", "", target, time, result, detailed);
            for (int i = 0; i < pointHealth.Length; i++)
            {
                target.Stats.HardPointDamage += pointHealth[i] - target.HardPoints[i].Health;
                if (pointHealth[i] > 0 && target.HardPoints[i].IsDestroyed)
                    Add("Hardpoint destroyed", target.Definition.HardPoints[i].Name, target, time, result, detailed);
            }
            if (target.IsDestroyed)
            {
                target.Stats.DestroyedAt = time;
                Add("Kill", impact.Source.Stats.Name, target, time, result, detailed);
            }
        }

        private static void Add(string kind, string detail, SimUnit unit, double time, BattleResult result, bool detailed)
        {
            if (detailed) result.Events.Add(new BattleEvent { Time = time, UnitIndex = unit.Stats.Index, Kind = kind, Detail = detail });
        }
    }
}
