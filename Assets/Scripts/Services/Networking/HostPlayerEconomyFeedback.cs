using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.Saves;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    public sealed class HostPlayerEconomyFeedback : IInitializable, ILateDisposable
    {
        private readonly IMatchSession _session;
        private readonly PlayerSlot _owner;
        private readonly OnlineMatchService _online;
        private readonly FactionService _faction;
        private readonly PlayerFactionModel _model;
        private readonly ReinforcementService _reinforcements;
        private readonly ReinforcementModel _capacity;

        public HostPlayerEconomyFeedback(IMatchSession session, PlayerSlot owner, OnlineMatchService online,
            FactionService faction, PlayerFactionModel model, ReinforcementService reinforcements,
            ReinforcementModel capacity)
        {
            _session = session;
            _owner = owner;
            _online = online;
            _faction = faction;
            _model = model;
            _reinforcements = reinforcements;
            _capacity = capacity;
        }

        public void Initialize()
        {
            if (!_session.IsOnline || !_session.IsAuthority) return;
            _faction.PurchaseAccepted += PurchaseAccepted;
            _faction.PurchaseCanceled += PurchaseCanceled;
            _faction.UnitProduced += UnitProduced;
            _faction.ResearchCompleted += ResearchCompleted;
            _faction.StationUpgraded += StationUpgraded;
            _reinforcements.ReinforcementDeployed += ReinforcementDeployed;
            _reinforcements.CapacityRejected += CapacityRejected;
        }

        public void LateDispose()
        {
            if (!_session.IsOnline || !_session.IsAuthority) return;
            _faction.PurchaseAccepted -= PurchaseAccepted;
            _faction.PurchaseCanceled -= PurchaseCanceled;
            _faction.UnitProduced -= UnitProduced;
            _faction.ResearchCompleted -= ResearchCompleted;
            _faction.StationUpgraded -= StationUpgraded;
            _reinforcements.ReinforcementDeployed -= ReinforcementDeployed;
            _reinforcements.CapacityRejected -= CapacityRejected;
        }

        private void PurchaseAccepted(UnitRequest request) => Send(FactionFeedbackKind.PurchaseAccepted, request);
        private void PurchaseCanceled(UnitRequest request) => Send(FactionFeedbackKind.PurchaseCanceled, request);
        private void UnitProduced(UnitRequest request) => Send(FactionFeedbackKind.UnitProduced, request);
        private void ResearchCompleted(ResearchType _) => Send(FactionFeedbackKind.ResearchCompleted);
        private void StationUpgraded() => Send(FactionFeedbackKind.StationUpgraded);
        private void ReinforcementDeployed(UnitRequest request) => Send(FactionFeedbackKind.ReinforcementDeployed, request);
        private void CapacityRejected() => Send(FactionFeedbackKind.CapacityRejected);

        private void Send(FactionFeedbackKind kind, UnitRequest request = null)
        {
            if (!_online.HasBegun) return;
            var message = new PlayerEconomyFeedbackMessage
            {
                Owner = _owner.Id,
                Kind = kind,
                UnitCapacity = _capacity.CurrentUnitCapacity,
                StationLevel = _model.CurrentLevel
            };
            if (request != null)
            {
                UnitRequestSave saved = UnitRequestSave.From(request);
                message.RequestKind = saved.Kind;
                message.RequestKey = saved.Key;
                message.RequestTier = saved.Tier;
            }
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (connection.connectionId != OnlineLobby.HOST_CONNECTION_ID && _online.IsBattleLoaded(connection) &&
                    _online.TryGetPlayer(connection.connectionId, out PlayerId player) && player == _owner.Id)
                    connection.Send(message);
            }
        }
    }
}
