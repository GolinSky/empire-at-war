using EmpireAtWar.Components.AttackComponent;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class SimMountDefinition
    {
        public WeaponProfile Profile;
        public int HardPointId;
        public int UnlockLevel = 1;
        public float DamageDuration;
        public float BeamDelay;
        public bool IsBeam;
    }
}
