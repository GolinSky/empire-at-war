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
        private readonly List<EnemyDefenseChoice> _topChoices = new List<EnemyDefenseChoice>(3);
        public IReadOnlyList<EnemyDefenseChoice> TopChoices => _topChoices;

        public bool TrySelect(EnemyDefenseSnapshot snapshot, out EnemyDefenseChoice choice)
        {
            _topChoices.Clear();
            choice = default;
            bool found = false;
            foreach (EnemyDefensePad pad in snapshot.Pads)
            {
                if (pad.HasHostiles || pad.Existing.Count >= 3 ||
                    snapshot.Strategic.Difficulty == EnemyAiDifficulty.Easy && pad.Existing.Count >= 1)
                    continue;
                float bestFit = 0f;
                foreach (EnemyDefenseOption option in snapshot.Options)
                    bestFit = Math.Max(bestFit, GetFit(snapshot, pad, option));
                if (bestFit <= 0f) continue;

                bool urgent = pad.IsThreatened || snapshot.Strategic.BaseThreatRatio >= 1f;
                float spendable = snapshot.Money - (urgent ? 0f : snapshot.ProductionReserve);
                float threshold = snapshot.Strategic.Difficulty == EnemyAiDifficulty.Easy ? 1f : 0.35f;
                if (snapshot.Strategic.Difficulty == EnemyAiDifficulty.UltraHard && urgent) threshold *= 0.5f;
                foreach (EnemyDefenseOption option in snapshot.Options)
                {
                    if (option.Cost > spendable) continue;
                    float affordability = 1f - option.Cost / Math.Max(1f, spendable + snapshot.IncomePerSecond * HORIZON);
                    int copies = 0;
                    foreach (SiteFacilityType existing in pad.Existing)
                        if (existing == option.Type) copies++;
                    float score = GetPadValue(snapshot, pad) * GetFit(snapshot, pad, option) / bestFit *
                                  affordability * (float)Math.Pow(0.6, copies);
                    string reason = urgent ? "Threatened point" : pad.ProtectedMineShare > 0f ? "Protect mining income" : "Secure exposed point";
                    EnemyDefenseChoice candidate = new EnemyDefenseChoice(pad.SiteIndex, option.Type, score, option.Cost, reason);
                    RecordTop(candidate);
                    if (score < threshold || found && score <= choice.Score) continue;
                    choice = candidate;
                    found = true;
                }
            }
            return found;
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
                float ownTime = CombatMatchup.TimeToDestroyClass(hull, shields, snapshot.Own.HullDps(target),
                    snapshot.Own.ShieldDps(target), snapshot.Own.PiercingDps(target));
                float combinedTime = CombatMatchup.TimeToDestroyClass(hull, shields,
                    snapshot.Own.HullDps(target) + option.Profile.HullDps(target),
                    snapshot.Own.ShieldDps(target) + option.Profile.ShieldDps(target),
                    snapshot.Own.PiercingDps(target) + option.Profile.PiercingDps(target));
                float ownDps = ownTime >= CombatMatchup.MAX_TIME ? 0f : durability / Math.Max(0.001f, ownTime);
                float combinedDps = combinedTime >= CombatMatchup.MAX_TIME ? 0f : durability / Math.Max(0.001f, combinedTime);
                float dps = Math.Max(0f, combinedDps - ownDps);
                float gap = snapshot.Strategic.Difficulty == EnemyAiDifficulty.Easy ? 0f :
                    Math.Max(0f, 1f - ownDps * HORIZON / durability);
                effect += durability / Math.Max(1f, total) * dps * (1f + gap);
            }
            // Under siege, reach adds time to fire before the attacking fleet closes on the defended point.
            float rangeValue = 1f + Math.Min(2f, option.Range / 500f) *
                (1f - pad.Exposure) * Math.Min(2f, snapshot.Strategic.BaseThreatRatio);
            return effect * rangeValue / option.Cost;
        }
    }
}
