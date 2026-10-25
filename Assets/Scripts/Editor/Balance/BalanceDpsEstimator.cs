using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;

namespace EmpireAtWar.Editor.Balance
{
    // Base DPS from draft values: mounts × each weapon's derived damage per second.
    // Excludes accuracy, damage matrix, research, abilities, firing arcs and projectile travel.
    public sealed class BalanceDpsEstimator : System.IDisposable
    {
        public const string FORMULA = "Base DPS = mounts × damage × shots per salvo / max(reload, (shots − 1) × shot interval), " +
                                      "with each weapon's stats derived from its family, caliber, mount, pattern and variant.";

        private readonly BalanceRegistration _registry;
        private readonly BalanceWeaponPreview _weapons;

        public BalanceDpsEstimator(BalanceRegistration registry)
        {
            _registry = registry;
            _weapons = new BalanceWeaponPreview(registry);
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
                WeaponType type = (WeaponType)int.Parse(group.Key, CultureInfo.InvariantCulture);
                if (!_weapons.TryGetProfile(type, draft, out WeaponProfile profile))
                    return BalanceDpsEstimate.Unavailable("Missing weapon profile: " + BalanceFieldView.Display(group.First(), group.Key));
                if (!(profile.CycleSeconds > 0))
                    return BalanceDpsEstimate.Unavailable(BalanceDpsEstimate.INVALID_DRAFT);

                total += group.Count() * (double)profile.DamagePerSecond;
            }

            return double.IsFinite(total)
                ? BalanceDpsEstimate.Of(total)
                : BalanceDpsEstimate.Unavailable(BalanceDpsEstimate.INVALID_DRAFT);
        }

        public void Dispose() => _weapons.Dispose();
    }
}
