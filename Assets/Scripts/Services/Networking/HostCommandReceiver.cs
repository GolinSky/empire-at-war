using System;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.PlayerCommands;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host: applies client commands through the same <see cref="AuthorityCommandSink"/> as local input.
    /// A command whose issuer is not the sending connection's slot is dropped before any game rule runs.
    /// </summary>
    public sealed class HostCommandReceiver : IInitializable, ILateDisposable
    {
        private readonly AuthorityCommandSink _authority;
        private readonly INetworkPlayerDirectory _players;

        public HostCommandReceiver(AuthorityCommandSink authority, INetworkPlayerDirectory players)
        {
            _authority = authority;
            _players = players;
        }

        public void Initialize() => NetworkServer.RegisterHandler<PlayerCommandMessage>(Receive);

        public void LateDispose() => NetworkServer.UnregisterHandler<PlayerCommandMessage>();

        private void Receive(NetworkConnectionToClient connection, PlayerCommandMessage message)
        {
            PlayerId issuer = IssuerOf(message);
            if (!_players.TryGetPlayer(connection.connectionId, out PlayerId player) || player != issuer)
            {
                Debug.LogWarning($"[Network] Dropped {message.Kind} from connection {connection.connectionId}: issuer {issuer} is not its player.");
                return;
            }

            Apply(_authority, message);
        }

        public static PlayerId IssuerOf(PlayerCommandMessage message) => message.Kind switch
        {
            PlayerCommandKind.UnitOrder => message.UnitOrder.Issuer,
            PlayerCommandKind.Purchase => message.Purchase.Issuer,
            PlayerCommandKind.CancelPurchase => message.CancelPurchase.Issuer,
            PlayerCommandKind.BuildSiteFacility => message.BuildSiteFacility.Issuer,
            PlayerCommandKind.FireSuperWeapon => message.FireSuperWeapon.Issuer,
            PlayerCommandKind.ShipAbility => message.ShipAbility.Issuer,
            PlayerCommandKind.DeployReinforcement => message.DeployReinforcement.Issuer,
            PlayerCommandKind.PlaceMarker => message.PlaceMarker.Issuer,
            _ => throw new ArgumentOutOfRangeException(nameof(message), message.Kind, "Unknown command kind.")
        };

        public static void Apply(IPlayerCommandSink sink, PlayerCommandMessage message)
        {
            switch (message.Kind)
            {
                case PlayerCommandKind.UnitOrder: sink.Submit(message.UnitOrder); break;
                case PlayerCommandKind.Purchase: sink.Submit(message.Purchase); break;
                case PlayerCommandKind.CancelPurchase: sink.Submit(message.CancelPurchase); break;
                case PlayerCommandKind.BuildSiteFacility: sink.Submit(message.BuildSiteFacility); break;
                case PlayerCommandKind.FireSuperWeapon: sink.Submit(message.FireSuperWeapon); break;
                case PlayerCommandKind.ShipAbility: sink.Submit(message.ShipAbility); break;
                case PlayerCommandKind.DeployReinforcement: sink.Submit(message.DeployReinforcement); break;
                case PlayerCommandKind.PlaceMarker: sink.Submit(message.PlaceMarker); break;
                default: throw new ArgumentOutOfRangeException(nameof(message), message.Kind, "Unknown command kind.");
            }
        }
    }
}
