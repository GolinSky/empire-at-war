using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.BattleNotifications;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Saves;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Registered only in the local human's scope, so there is one global message handler.</summary>
    public sealed class ClientPlayerEconomyFeedback : IInitializable, ILateDisposable
    {
        private readonly IMatchSession _session;
        private readonly FactionAnnouncementService _announcements;
        private readonly IUnitRequestFactory _requests;
        private readonly FactionRoster _roster;
        private readonly StationLevelData _levels;
        private readonly EconomyNotifier _notifications;

        public ClientPlayerEconomyFeedback(IMatchSession session, FactionAnnouncementService announcements,
            IUnitRequestFactory requests, FactionRoster roster, StationLevelData levels, EconomyNotifier notifications)
        {
            _session = session;
            _announcements = announcements;
            _notifications = notifications;
            _requests = requests;
            _roster = roster;
            _levels = levels;
        }

        public void Initialize()
        {
            if (_session.IsOnline && !_session.IsAuthority)
                NetworkClient.RegisterHandler<PlayerEconomyFeedbackMessage>(Receive);
        }

        public void LateDispose()
        {
            if (_session.IsOnline && !_session.IsAuthority)
                NetworkClient.UnregisterHandler<PlayerEconomyFeedbackMessage>();
        }

        public void Receive(PlayerEconomyFeedbackMessage message)
        {
            if (_session.IsAuthority || message.Owner != _session.LocalPlayerId) return;
            UnitRequest request = null;
            if (message.Kind == FactionFeedbackKind.PurchaseAccepted ||
                message.Kind == FactionFeedbackKind.PurchaseCanceled ||
                message.Kind == FactionFeedbackKind.UnitProduced ||
                message.Kind == FactionFeedbackKind.ReinforcementDeployed)
                request = UnitRequestSave.CreateRequest(message.RequestKind, message.RequestKey,
                    message.RequestTier, _requests, _roster, _levels);
            _announcements.AnnounceReplicated(message.Kind, request, message.UnitCapacity, message.StationLevel);
            _notifications.ShowReplicated(message.Kind, request, message.StationLevel);
        }
    }
}
