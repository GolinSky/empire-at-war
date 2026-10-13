using System.Collections.Generic;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class SpreadTargetPolicy : ITargetPolicy
    {
        public SimUnit Select(SimUnit attacker, IReadOnlyList<SimUnit> candidates) =>
            candidates[attacker.TargetCursor++ % candidates.Count];
    }
}
