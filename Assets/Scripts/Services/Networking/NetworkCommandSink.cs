using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.PlayerCommands;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Online client: every command goes to the host (reliable); nothing is applied locally.</summary>
    public sealed class NetworkCommandSink : IPlayerCommandSink
    {
        private readonly ClientReplicaBuffer _replica;

        public NetworkCommandSink(ClientReplicaBuffer replica)
        {
            _replica = replica;
        }

        public void Submit(UnitOrderCommand command) =>
            Send(new PlayerCommandMessage { Kind = PlayerCommandKind.UnitOrder, UnitOrder = command });

        public void Submit(PurchaseUnitCommand command) =>
            Send(new PlayerCommandMessage { Kind = PlayerCommandKind.Purchase, Purchase = command });

        public void Submit(CancelPurchaseCommand command) =>
            Send(new PlayerCommandMessage { Kind = PlayerCommandKind.CancelPurchase, CancelPurchase = command });

        public void Submit(BuildSiteFacilityCommand command) =>
            Send(new PlayerCommandMessage { Kind = PlayerCommandKind.BuildSiteFacility, BuildSiteFacility = command });

        public void Submit(FireSuperWeaponCommand command) =>
            Send(new PlayerCommandMessage { Kind = PlayerCommandKind.FireSuperWeapon, FireSuperWeapon = command });

        public void Submit(ShipAbilityCommand command) =>
            Send(new PlayerCommandMessage { Kind = PlayerCommandKind.ShipAbility, ShipAbility = command });

        public void Submit(DeployReinforcementCommand command) =>
            Send(new PlayerCommandMessage { Kind = PlayerCommandKind.DeployReinforcement, DeployReinforcement = command });

        private void Send(PlayerCommandMessage message)
        {
            message.LastReceivedTick = _replica.LastReceivedTick;
            NetworkClient.Send(message);
        }
    }
}
