using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class SimHardPointDefinition
    {
        public HardPointType Type;
        public int UnlockLevel = 1;
        public float HealthOverride;
        public string Name;
    }
}
