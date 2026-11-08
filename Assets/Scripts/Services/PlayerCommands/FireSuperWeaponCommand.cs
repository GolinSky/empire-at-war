using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.PlayerCommands
{
    public readonly struct FireSuperWeaponCommand
    {
        public PlayerId Issuer { get; }
        public SuperWeaponType Type { get; }
        public long TargetId { get; }

        public FireSuperWeaponCommand(PlayerId issuer, SuperWeaponType type, long targetId)
        {
            Issuer = issuer;
            Type = type;
            TargetId = targetId;
        }
    }
}
