using EmpireAtWar.Components.AttackComponent;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class SimImpact
    {
        public double Time;
        public SimUnit Source;
        public SimUnit Target;
        public int HardPointId;
        public float Damage;
        public DamageType DamageType;
    }
}
