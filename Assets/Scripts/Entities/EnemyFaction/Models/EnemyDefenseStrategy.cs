using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public sealed class EnemyDefenseStrategy
    {
        private const float HORIZON = 30f;
        public const int ZONE_SATELLITES = 3;
        private const int MINING_POINT_SATELLITES = 1;
        private const float SQUADRON_HEAVY_SHARE = 0.4f;
        private const float RICH_MONEY_FACTOR = 1.5f;
        private readonly List<EnemyDefenseChoice> _topChoices = new List<EnemyDefenseChoice>(3);
        public IReadOnlyList<EnemyDefenseChoice> TopChoices => _topChoices;

        /// <summary>Required satellites first, then the best scored choice.</summary>
        public bool TrySelect(EnemyDefenseSnapshot snapshot, out EnemyDefenseChoice choice)
        {
            _topChoices.Clear();
            return TrySelectMandatory(snapshot, out choice) || TrySelectScored(snapshot, out choice);
        }

        /// <summary>The threat- and income-scored choice among pads that already meet their required satellites.</summary>
        public bool TrySelectScored(EnemyDefenseSnapshot snapshot, out EnemyDefenseChoice choice)
        {
            _topChoices.Clear();
            choice = default;
            bool found = false;
            foreach (EnemyDefensePad pad in snapshot.Pads)
            {
                if (pad.HasHostiles || CountArmed(pad) >= ZONE_SATELLITES)
                    continue;
                float bestFit = 0f;
                foreach (EnemyDefenseOption option in snapshot.Options)
                    bestFit = Math.Max(bestFit, GetFit(snapshot, pad, option));
                if (bestFit <= 0f) continue;

                bool urgent = pad.IsThreatened || snapshot.Strategic.BaseThreatRatio >= 1f;
                float spendable = snapshot.Money - (urgent ? 0f : snapshot.ProductionReserve);
                foreach (EnemyDefenseOption option in snapshot.Options)
                {
                    if (option.Type == SiteFacilityType.RepairSatellite || option.Cost > spendable) continue;
                    float affordability = 1f - option.Cost / Math.Max(1f, spendable + snapshot.IncomePerSecond * HORIZON);
                    int copies = 0;
                    foreach (SiteFacilityType existing in pad.Existing)
                        if (existing == option.Type) copies++;
                    float score = GetPadValue(snapshot, pad) * GetFit(snapshot, pad, option) / bestFit *
                                  affordability * (float)Math.Pow(0.6, copies);
                    string reason = urgent ? "Threatened point" : pad.ProtectedMineShare > 0f ? "Protect mining income" : "Secure exposed point";
                    EnemyDefenseChoice candidate = new EnemyDefenseChoice(pad.SiteIndex, option.Type, score, option.Cost, reason);
                    RecordTop(candidate);
                    if (score < 0.35f || found && score <= choice.Score) continue;
                    choice = candidate;
                    found = true;
                }
            }
            return found;
        }

        /// <summary>
        /// Every owned pad's point gets its required satellites before any scored choice: three at a mine-less zone
        /// (relay or centre), one at a mining point. Money reserved for production does not apply.
        /// </summary>
        public bool TrySelectMandatory(EnemyDefenseSnapshot snapshot, out EnemyDefenseChoice choice)
        {
            choice = default;
            bool found = false;
            foreach (EnemyDefensePad pad in snapshot.Pads)
            {
                if (!NeedsMandatorySatellite(pad)) continue;
                if (!TryPickType(snapshot, pad, out EnemyDefenseOption option)) continue;
                float score = GetPointPriority(pad);
                if (found && score <= choice.Score) continue;
                choice = new EnemyDefenseChoice(pad.SiteIndex, option.Type, score, option.Cost,
                    pad.IsZonePad ? "Hold the zone with satellites" : "Mandatory mining-point satellite");
                found = true;
            }
            return found;
        }

        /// <summary>
        /// Money beyond the production reserve fills every remaining empty owned pad, centre first, so no captured
        /// pad stays empty while credits pile up.
        /// </summary>
        public bool TrySelectFill(EnemyDefenseSnapshot snapshot, out EnemyDefenseChoice choice)
        {
            choice = default;
            bool found = false;
            float spendable = snapshot.Money - snapshot.ProductionReserve;
            foreach (EnemyDefensePad pad in snapshot.Pads)
            {
                if (pad.HasHostiles || !TryPickType(snapshot, pad, out EnemyDefenseOption option) ||
                    option.Cost > spendable) continue;
                float score = GetPointPriority(pad);
                if (found && score <= choice.Score) continue;
                choice = new EnemyDefenseChoice(pad.SiteIndex, option.Type, score, option.Cost, "Fill owned pad");
                found = true;
            }
            return found;
        }

        /// <summary>
        /// One repair satellite per pad group with money beyond the production reserve: only into a point with at least
        /// two pads that already holds an armed satellite (built or constructing), and only where a team structure stands
        /// within its reach. A single-pad point never gets one. It has no weapons, so the scored choice never picks it.
        /// </summary>
        public bool TrySelectRepair(EnemyDefenseSnapshot snapshot, out EnemyDefenseChoice choice)
        {
            choice = default;
            if (!TryGetOption(snapshot, SiteFacilityType.RepairSatellite, out EnemyDefenseOption repair) ||
                repair.Cost > snapshot.Money - snapshot.ProductionReserve) return false;
            bool found = false;
            foreach (EnemyDefensePad pad in snapshot.Pads)
            {
                if (!CanTakeRepairSatellite(pad)) continue;
                float score = GetPointPriority(pad);
                if (found && score <= choice.Score) continue;
                choice = new EnemyDefenseChoice(pad.SiteIndex, repair.Type, score, repair.Cost,
                    "Repair satellite beside armed satellites");
                found = true;
            }
            return found;
        }

        public static bool CanTakeRepairSatellite(EnemyDefensePad pad) =>
            !pad.HasHostiles && pad.PointPads >= 2 && pad.HasTeamStructureInRepairRange && CountArmed(pad) >= 1 &&
            !Contains(pad.Existing, SiteFacilityType.RepairSatellite);

        /// <summary>Weapon satellites of the pad's point, built or constructing; a repair satellite is not one.</summary>
        public static int CountArmed(EnemyDefensePad pad)
        {
            int armed = 0;
            foreach (SiteFacilityType existing in pad.Existing)
                if (existing != SiteFacilityType.RepairSatellite) armed++;
            return armed;
        }

        private static bool Contains(IReadOnlyList<SiteFacilityType> types, SiteFacilityType type)
        {
            foreach (SiteFacilityType existing in types)
                if (existing == type) return true;
            return false;
        }

        private static bool TryGetOption(EnemyDefenseSnapshot snapshot, SiteFacilityType type, out EnemyDefenseOption found)
        {
            foreach (EnemyDefenseOption option in snapshot.Options)
            {
                if (option.Type != type) continue;
                found = option;
                return true;
            }
            found = default;
            return false;
        }

        /// <summary>Threatened point → home → held zone (centre first) → mining point → free pad.</summary>
        private static float GetPointPriority(EnemyDefensePad pad) =>
            (pad.IsThreatened ? 1000f : 0f) + (pad.IsHomePad ? 100f : 0f) +
            (pad.IsCentrePad ? 20f : pad.IsZonePad ? 10f : 0f) + pad.ProtectedMineShare - (pad.IsFreePad ? 1f : 0f);

        public bool HasMandatoryPad(EnemyDefenseSnapshot snapshot)
        {
            foreach (EnemyDefensePad pad in snapshot.Pads)
                if (NeedsMandatorySatellite(pad)) return true;
            return false;
        }

        private static bool NeedsMandatorySatellite(EnemyDefensePad pad) => !pad.HasHostiles && !pad.IsFreePad &&
            CountArmed(pad) < (pad.IsZonePad ? ZONE_SATELLITES : MINING_POINT_SATELLITES);

        // Turbolaser by default; quad laser when squadrons dominate; missile or mass driver once money is plentiful.
        private static bool TryPickType(EnemyDefenseSnapshot snapshot, EnemyDefensePad pad, out EnemyDefenseOption picked)
        {
            picked = default;
            bool hasPicked = false;
            float squadronShare = GetSquadronShare(snapshot.Hostile);
            SiteFacilityType wanted = squadronShare >= SQUADRON_HEAVY_SHARE
                ? SiteFacilityType.QuadLaserSatellite : SiteFacilityType.TurbolaserSatellite;
            EnemyDefenseOption heavy = default;
            bool hasHeavy = false;
            float heavyFit = 0f;
            foreach (EnemyDefenseOption option in snapshot.Options)
            {
                if (option.Type == wanted) { picked = option; hasPicked = true; }
                if (squadronShare >= SQUADRON_HEAVY_SHARE || option.Type != SiteFacilityType.MissileSatellite &&
                    option.Type != SiteFacilityType.MassDriverSatellite) continue;
                float fit = GetFit(snapshot, pad, option);
                if (fit <= heavyFit) continue;
                heavy = option;
                hasHeavy = true;
                heavyFit = fit;
            }
            if (!hasPicked) return false;
            if (hasHeavy && snapshot.Money >= heavy.Cost * RICH_MONEY_FACTOR && pad.Existing.Count >= 1) picked = heavy;
            return picked.Cost <= snapshot.Money;
        }

        private static float GetSquadronShare(ForceComposition hostile)
        {
            float total = 0f;
            float squadrons = 0f;
            foreach (ShipClass target in ForceComposition.Classes)
            {
                float durability = hostile.Hull(target) + hostile.Shields(target);
                total += durability;
                if (target == ShipClass.Fighter || target == ShipClass.Bomber || target == ShipClass.Interceptor)
                    squadrons += durability;
            }
            return total > 0f ? squadrons / total : 0f;
        }

        private void RecordTop(EnemyDefenseChoice choice)
        {
            int index = 0;
            while (index < _topChoices.Count && _topChoices[index].Score >= choice.Score) index++;
            if (index >= 3) return;
            _topChoices.Insert(index, choice);
            if (_topChoices.Count > 3) _topChoices.RemoveAt(3);
        }

        private static float GetPadValue(EnemyDefenseSnapshot snapshot, EnemyDefensePad pad)
        {
            float recent = Math.Max(0f, 1f - pad.SecondsSinceHostile / HORIZON);
            float homePressure = (1f - pad.Exposure) * snapshot.Strategic.BaseThreatRatio;
            float outmatched = 1f / Math.Max(1f, snapshot.Strategic.FleetAdvantage);
            float localPressure = pad.IsThreatened ? 1f / Math.Max(0.25f, snapshot.Strategic.LocalAdvantage) : 0f;
            float value = pad.ProtectedMineShare + pad.Exposure + 3f * homePressure + recent + localPressure;
            return value * (1f + outmatched) / (1f + pad.Existing.Count);
        }

        private static float GetFit(EnemyDefenseSnapshot snapshot, EnemyDefensePad pad, EnemyDefenseOption option)
        {
            // Until scouts provide composition, the generalist is the conservative default.
            if (snapshot.Hostile.IsEmpty) return option.Type == SiteFacilityType.TurbolaserSatellite ? 1f : 0f;
            float total = 0f;
            foreach (ShipClass target in ForceComposition.Classes)
                total += snapshot.Hostile.Hull(target) + snapshot.Hostile.Shields(target);
            float effect = 0f;
            foreach (ShipClass target in ForceComposition.Classes)
            {
                float hull = snapshot.Hostile.Hull(target);
                float shields = snapshot.Hostile.Shields(target);
                float durability = hull + shields;
                if (durability <= 0f) continue;
                float regen = snapshot.Hostile.ShieldRegenPerUnit(target);
                float repair = snapshot.Hostile.HullRepairPerUnit(target);
                float ownTime = CombatMatchup.TimeToDestroyClass(hull, shields, snapshot.Own.HullDps(target),
                    snapshot.Own.ShieldDps(target), snapshot.Own.PiercingDps(target), regen, repair);
                float combinedTime = CombatMatchup.TimeToDestroyClass(hull, shields,
                    snapshot.Own.HullDps(target) + option.Profile.HullDps(target),
                    snapshot.Own.ShieldDps(target) + option.Profile.ShieldDps(target),
                    snapshot.Own.PiercingDps(target) + option.Profile.PiercingDps(target), regen, repair);
                float ownDps = ownTime >= CombatMatchup.MAX_TIME ? 0f : durability / Math.Max(0.001f, ownTime);
                float combinedDps = combinedTime >= CombatMatchup.MAX_TIME ? 0f : durability / Math.Max(0.001f, combinedTime);
                float dps = Math.Max(0f, combinedDps - ownDps);
                float gap = Math.Max(0f, 1f - ownDps * HORIZON / durability);
                effect += durability / Math.Max(1f, total) * dps * (1f + gap);
            }
            // Under siege, reach adds time to fire before the attacking fleet closes on the defended point.
            float rangeValue = 1f + Math.Min(2f, option.Range / 500f) *
                (1f - pad.Exposure) * Math.Min(2f, snapshot.Strategic.BaseThreatRatio);
            return effect * rangeValue / option.Cost;
        }
    }
}
