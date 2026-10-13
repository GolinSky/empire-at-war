using System;

namespace EmpireAtWar.Editor.BattleSimulator
{
    [Serializable]
    public sealed class BattleFleetEntry
    {
        public string UnitId;
        public int Quantity = 1;
        public int Level = 1;
        public float ArcFactor = 1;
    }
}
