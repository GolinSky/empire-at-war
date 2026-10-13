using System.Collections.Generic;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class BattleResult
    {
        public int Seed;
        public int Winner = -1;
        public string Reason;
        public double Duration;
        public readonly List<UnitCombatStats> Units = new List<UnitCombatStats>();
        public readonly List<BattleEvent> Events = new List<BattleEvent>();
        public readonly List<BattleSample> Timeline = new List<BattleSample>();
    }
}
