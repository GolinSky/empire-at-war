using System.Collections.Generic;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class SameClassTargetPolicy : ITargetPolicy
    {
        private readonly WeakestTargetPolicy _fallback = new WeakestTargetPolicy();
        public SimUnit Select(SimUnit attacker, IReadOnlyList<SimUnit> candidates)
        {
            SimUnit selected = null;
            foreach (SimUnit candidate in candidates)
                if (candidate.Definition.ShipClass == attacker.Definition.ShipClass &&
                    (selected == null || candidate.Hull + candidate.Shields < selected.Hull + selected.Shields)) selected = candidate;
            return selected ?? _fallback.Select(attacker, candidates);
        }
    }
}
