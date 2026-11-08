using EmpireAtWar.Entities.SuperWeapons;
using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → clients, reliable: a superweapon salvo was fired at a target.</summary>
    public struct SuperWeaponFiredMessage : NetworkMessage
    {
        public SuperWeaponType Type;
        public long TargetId;
    }
}
