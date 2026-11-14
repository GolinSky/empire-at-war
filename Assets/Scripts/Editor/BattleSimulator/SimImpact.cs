using EmpireAtWar.Components.AttackComponent;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public struct SimImpact
    {
        public SimUnit Source;
        public SimUnit Target;
        public int HardPointId;
        public float Damage;
        public DamageType DamageType;
    }
}
