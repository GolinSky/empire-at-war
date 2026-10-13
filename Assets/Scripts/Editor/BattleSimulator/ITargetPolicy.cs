using System.Collections.Generic;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public interface ITargetPolicy
    {
        SimUnit Select(SimUnit attacker, IReadOnlyList<SimUnit> candidates);
    }
}
