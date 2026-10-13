using System;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public static class BattleDamageResolver
    {
        public static void Apply(SimImpact impact, double time, BattleResult result, bool detailed)
        {
            SimUnit target = impact.Target;
            var point = target.HardPoints.Length == 0 ? null : target.HardPoints[impact.HardPointId];
            // Runtime AttackData.CanTarget drops impacts on a point destroyed in flight while other points live.
            if (target.IsDestroyed || (point != null && point.IsDestroyed && target.HasLiveHardPoints))
            {
                impact.Source.Stats.Overkill += impact.Damage;
                return;
            }
            float hull = target.Hull;
            float shields = target.Shields;
            float pointHealth = point == null ? 0 : point.Health;
            bool lostGenerator = target.LostGenerator;
            target.ApplyDamage(impact.Damage, impact.DamageType, impact.HardPointId);
            double hullDamage = hull - target.Hull;
            double shieldDamage = shields - target.Shields;
            target.Stats.HullDamage += hullDamage;
            target.Stats.ShieldDamage += shieldDamage;
            target.Stats.HardPointDamage += point == null ? 0 : pointHealth - point.Health;
            impact.Source.Stats.DamageDealt += hullDamage + shieldDamage;
            if (shields > 0 && target.Shields == 0) Add("Shields down", "", target, time, result, detailed);
            if (!lostGenerator && target.LostGenerator) Add("Shield generator lost", "", target, time, result, detailed);
            if (point != null && pointHealth > 0 && point.IsDestroyed)
                Add("Hardpoint destroyed", target.Definition.HardPoints[point.Id].Name, target, time, result, detailed);
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
