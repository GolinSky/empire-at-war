using System.Collections.Generic;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class WeakestTargetPolicy : ITargetPolicy
    {
        public SimUnit Select(SimUnit attacker, IReadOnlyList<SimUnit> candidates)
        {
            SimUnit selected = candidates[0];
            foreach (SimUnit candidate in candidates)
                if (candidate.Hull + candidate.Shields < selected.Hull + selected.Shields) selected = candidate;
            return selected;
        }
    }
}
