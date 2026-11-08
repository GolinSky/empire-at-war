using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>
    /// Host → clients, reliable: a weapon's target changed (radar, AI or order). <see cref="Reset"/> drops the main
    /// target; otherwise the shooter adds <see cref="TargetId"/> as <see cref="AttackType"/>, focused on
    /// <see cref="HardPointId"/> (-1 = any of <see cref="HardPointType"/>).
    /// </summary>
    public struct WeaponTargetMessage : NetworkMessage
    {
        public long ShooterId;
        public bool Reset;
        public long TargetId;
        public AttackType AttackType;
        public int HardPointId;
        public HardPointType HardPointType;
    }
}
