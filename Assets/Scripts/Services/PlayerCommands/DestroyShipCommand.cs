using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.PlayerCommands
{
    public readonly struct DestroyShipCommand
    {
        public PlayerId Issuer { get; }
        public long TargetId { get; }

        public DestroyShipCommand(PlayerId issuer, long targetId)
        {
            Issuer = issuer;
            TargetId = targetId;
        }
    }
}
