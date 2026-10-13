using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Editor.BattleSimulator
{
    [Serializable]
    public sealed class BattleSetup
    {
        public List<BattleFleetEntry> SideA = new List<BattleFleetEntry>();
        public List<BattleFleetEntry> SideB = new List<BattleFleetEntry>();
        public BattleTargetPolicy PolicyA;
        public BattleTargetPolicy PolicyB;
        public HardPointType HardPointFocus = HardPointType.Any;
        public int Seed = 1;
        public int Runs = 1;
        public float TimeLimit = 600;
        public bool AlphaStrike;
        public float Distance = -1;
        public bool UseDraft;
    }
}
