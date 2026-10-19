using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    [Serializable]
    public sealed class ResearchSave
    {
        [SerializeField] private ResearchType type;
        [SerializeField] private int completedTiers;

        public ResearchType Type => type;
        public int CompletedTiers => completedTiers;

        public static ResearchSave Create(ResearchType type, int completedTiers) =>
            new ResearchSave { type = type, completedTiers = completedTiers };

        public static ResearchSave[] CaptureAll(FactionResearchModel research) => research.ResearchTypes
            .Select(researchType => Create(researchType, research.GetCompletedTiers(researchType)))
            .Where(save => save.completedTiers > 0)
            .ToArray();

        /// <summary>Completes each saved tier again, so the research bonuses are rebuilt the way they were earned.</summary>
        public static void RestoreAll(IEnumerable<ResearchSave> saves, FactionResearchModel research)
        {
            foreach (ResearchSave save in saves)
                for (int tier = research.GetCompletedTiers(save.type); tier < save.completedTiers; tier++)
                    research.Complete(save.type);
        }
    }
}
