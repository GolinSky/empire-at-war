using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EmpireAtWar.Editor.Balance
{
    // Base DPS from draft values: mounts × damage × shots per salvo / firing cycle.
    // Excludes accuracy, damage matrix, research, abilities, firing arcs and projectile travel.
    public sealed class BalanceDpsEstimator
    {
        public const string FORMULA = "Base DPS = mounts × damage × shots per salvo / max(reload, (shots − 1) × shot interval).";

        private readonly BalanceRegistration _registry;
        private readonly Dictionary<string, BalanceField> _profiles;

        public BalanceDpsEstimator(BalanceRegistration registry)
        {
            _registry = registry;
            _profiles = registry.Fields.Values.Where(field => field.Stat.StartsWith("weapon/"))
                .ToDictionary(field => field.Stat);
        }

        public BalanceDpsEstimate Estimate(BalanceUnit unit, BalanceDraft draft)
        {
            List<BalanceField> mounts = unit.Mounts.Distinct()
                .SelectMany(_registry.FieldsFor)
                .Where(field => !field.SharedMountSource && field.Stat == "WeaponType")
                .ToList();
            if (mounts.Count == 0) return BalanceDpsEstimate.Unavailable(BalanceDpsEstimate.NOT_ARMED);

            double total = 0;
            foreach (IGrouping<string, BalanceField> group in mounts.GroupBy(field => field.DraftValue(draft)))
            {
                string prefix = "weapon/" + group.Key + "/";
                if (!_profiles.ContainsKey(prefix + "damage"))
                    return BalanceDpsEstimate.Unavailable("Missing weapon profile: " + BalanceFieldView.Display(group.First(), group.Key));

                double reload = Read(prefix + "reload", draft);
                double interval = Read(prefix + "shotInterval", draft);
                int shots = (int)Read(prefix + "shotsPerSalvo", draft);
                if (!(BalanceWeaponMath.CycleSeconds(shots, interval, reload) > 0))
                    return BalanceDpsEstimate.Unavailable(BalanceDpsEstimate.INVALID_DRAFT);

                total += group.Count() * BalanceWeaponMath.SalvoDps(Read(prefix + "damage", draft), shots, interval, reload);
            }

            return double.IsFinite(total)
                ? BalanceDpsEstimate.Of(total)
                : BalanceDpsEstimate.Unavailable(BalanceDpsEstimate.INVALID_DRAFT);
        }

        private double Read(string stat, BalanceDraft draft) =>
            double.Parse(_profiles[stat].DraftValue(draft), CultureInfo.InvariantCulture);
    }
}
