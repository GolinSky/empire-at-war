using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Squadrons.Data;

namespace EmpireAtWar.Editor.Balance
{
    // Base DPS from draft values: mounts × each weapon's derived damage per second.
    // Excludes accuracy, damage matrix, research, abilities, firing arcs and projectile travel.
    // Damage vs class adds the draft damage matrix: accuracy and hull damage against one target class.
    public sealed class BalanceDpsEstimator : System.IDisposable
    {
        public const string FORMULA = "Base DPS = mounts × damage × shots per salvo / max(reload, (shots − 1) × shot interval), " +
                                      "including squadron warhead tuning.";
        public const string VS_CLASS_FORMULA = "Damage vs class = Base DPS per weapon × min(1, matrix accuracy × size bias) × " +
                                               "matrix hull damage × size bias. Shields, range, firing arcs, turning and travel are excluded.";

        /// <summary>Target classes shown next to Base DPS: strikecraft, small, medium and large ships.</summary>
        public static readonly ShipClass[] DisplayedClasses =
            { ShipClass.Fighter, ShipClass.Corvette, ShipClass.Frigate, ShipClass.Capital };

        private readonly BalanceRegistration _registry;
        private readonly BalanceWeaponPreview _weapons;
        private readonly Dictionary<string, BalanceField> _matrix;

        public BalanceDpsEstimator(BalanceRegistration registry)
        {
            _registry = registry;
            _weapons = new BalanceWeaponPreview(registry);
            _matrix = registry.FieldsFor(registry.Resolve<DamageMatrixData>()).ToDictionary(field => field.Stat);
        }

        public BalanceDpsEstimate Estimate(BalanceUnit unit, BalanceDraft draft) =>
            Sum(unit, draft, profile => profile.DamagePerSecond);

        public BalanceDpsEstimate EstimateAgainst(BalanceUnit unit, BalanceDraft draft, ShipClass targetClass) =>
            Sum(unit, draft, profile => WeaponEngagementRange.EffectiveDamagePerSecond(profile, targetClass,
                MatrixValue(draft, profile.DamageType, "accuracy", targetClass),
                MatrixValue(draft, profile.DamageType, "damage", targetClass)));

        /// <summary>"Fighter 12 · Corvette 40 · …" for <see cref="DisplayedClasses"/>, or the problem when unavailable.</summary>
        public string DescribeAgainstClasses(BalanceUnit unit, BalanceDraft draft)
        {
            List<string> parts = new List<string>(DisplayedClasses.Length);
            foreach (ShipClass targetClass in DisplayedClasses)
            {
                BalanceDpsEstimate estimate = EstimateAgainst(unit, draft, targetClass);
                if (!estimate.HasValue) return estimate.Problem;
                parts.Add(targetClass + " " + estimate);
            }
            return string.Join(" · ", parts);
        }

        private BalanceDpsEstimate Sum(BalanceUnit unit, BalanceDraft draft, Func<WeaponProfile, float> damagePerSecond)
        {
            List<BalanceField> mounts = unit.Mounts.Distinct()
                .SelectMany(_registry.FieldsFor)
                .Where(field => !field.SharedMountSource && field.Stat == "WeaponType")
                .ToList();
            if (mounts.Count == 0) return BalanceDpsEstimate.Unavailable(BalanceDpsEstimate.NOT_ARMED);

            SquadronData squadron = unit.Data as SquadronData;
            double total = 0;
            foreach (IGrouping<string, BalanceField> group in mounts.GroupBy(field => field.DraftValue(draft)))
            {
                WeaponType type = (WeaponType)int.Parse(group.Key, CultureInfo.InvariantCulture);
                if (!_weapons.TryGetProfile(type, draft, out WeaponProfile profile))
                    return BalanceDpsEstimate.Unavailable("Missing weapon profile: " + BalanceFieldView.Display(group.First(), group.Key));
                if (squadron != null)
                    profile = profile.WithWarheadMultipliers(squadron.WarheadDamageMultiplier, squadron.WarheadReloadMultiplier);
                if (!(profile.CycleSeconds > 0))
                    return BalanceDpsEstimate.Unavailable(BalanceDpsEstimate.INVALID_DRAFT);

                total += group.Count() * (double)damagePerSecond(profile);
            }

            return double.IsFinite(total)
                ? BalanceDpsEstimate.Of(total)
                : BalanceDpsEstimate.Unavailable(BalanceDpsEstimate.INVALID_DRAFT);
        }

        private float MatrixValue(BalanceDraft draft, DamageType damageType, string kind, ShipClass targetClass) =>
            float.Parse(_matrix["matrix/" + (int)damageType + "/" + kind + "/" + targetClass].DraftValue(draft),
                CultureInfo.InvariantCulture);

        public void Dispose() => _weapons.Dispose();
    }
}
