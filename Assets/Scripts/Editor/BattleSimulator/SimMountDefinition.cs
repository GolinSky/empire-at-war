using EmpireAtWar.Components.AttackComponent;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class SimMountDefinition
    {
        public WeaponProfile Profile;
        public int HardPointId;
        public int UnlockLevel = 1;
        public float DamageDuration;
        // Beams and ion pulses reach the target after a fixed time; other shots fly distance / projectile speed.
        public bool HasFixedFlight;
        public float FixedFlightSeconds;
    }
}
