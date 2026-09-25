using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Factions
{
    /// <summary>
    /// Completed research of one faction. Each research line applies only the effects of its latest
    /// completed tier; effects of different lines stack multiplicatively.
    /// </summary>
    public sealed class FactionResearchModel : PureModel, IFactionResearchModelObserver
    {
        private readonly Dictionary<ResearchType, ResearchLineData> _lines;
        private readonly Dictionary<ResearchType, int> _completedTiers = new();
        private readonly Dictionary<(ResearchStat, ShipClass), float> _multipliers = new();

        public event Action<ResearchType> OnResearchCompleted;

        public IReadOnlyCollection<ResearchType> ResearchTypes => _lines.Keys;
        public float IncomeMultiplier { get; private set; } = 1f;

        public FactionResearchModel(FactionsData factionsData, FactionType factionType)
        {
            _lines = factionsData.GetResearchLines(factionType);
        }

        public bool TryGetNextTier(ResearchType researchType, out ResearchTierData tier)
        {
            ResearchTierData[] tiers = _lines[researchType].Tiers;
            int completedTiers = GetCompletedTiers(researchType);
            tier = completedTiers < tiers.Length ? tiers[completedTiers] : null;
            return tier != null;
        }

        public void Complete(ResearchType researchType)
        {
            if (!TryGetNextTier(researchType, out _))
            {
                throw new InvalidOperationException($"{researchType} research has no tiers left.");
            }

            _completedTiers[researchType] = GetCompletedTiers(researchType) + 1;
            RecalculateMultipliers();
            OnResearchCompleted?.Invoke(researchType);
        }

        public float GetMultiplier(ResearchStat stat, ShipClass shipClass)
        {
            return _multipliers.TryGetValue((stat, shipClass), out float multiplier) ? multiplier : 1f;
        }

        private int GetCompletedTiers(ResearchType researchType)
        {
            return _completedTiers.TryGetValue(researchType, out int completedTiers) ? completedTiers : 0;
        }

        private void RecalculateMultipliers()
        {
            _multipliers.Clear();
            IncomeMultiplier = 1f;
            foreach (KeyValuePair<ResearchType, int> completed in _completedTiers)
            {
                foreach (ResearchEffect effect in _lines[completed.Key].Tiers[completed.Value - 1].Effects)
                {
                    if (effect.Stat == ResearchStat.Income)
                    {
                        IncomeMultiplier *= effect.Multiplier;
                        continue;
                    }

                    foreach (ShipClass shipClass in effect.ShipClasses)
                    {
                        _multipliers[(effect.Stat, shipClass)] =
                            GetMultiplier(effect.Stat, shipClass) * effect.Multiplier;
                    }
                }
            }
        }
    }
}
