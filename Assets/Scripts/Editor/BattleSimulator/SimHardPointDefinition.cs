using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class SimHardPointDefinition
    {
        public HardPointType Type;
        public int UnlockLevel = 1;
        public float HealthWeight = 1f;
        public string Name;
    }
}
