using System;
using System.Linq;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Economy;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.Saves;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.States
{
    /// <summary>
    /// One human's credits, income timer, production queue and research, sent only to that player. Their client
    /// pays no income and advances no production; purchases already go to the host as commands.
    /// </summary>
    public sealed class PlayerEconomyReplicatedState : ReplicatedState
    {
        private readonly PlayerSlot _owner;
        private readonly EconomyModel _economy;
        private readonly EconomyService _economyService;
        private readonly PlayerFactionModel _faction;
        private readonly FactionService _factionService;
        private readonly FactionResearchModel _research;
        private readonly IUnitRequestFactory _requests;
        private readonly FactionRoster _roster;
        private readonly StationLevelData _levels;

        public PlayerEconomyReplicatedState(ReplicatedStateRegistry registry, IMatchSession session, PlayerSlot owner,
            EconomyModel economy, EconomyService economyService, PlayerFactionModel faction,
            FactionService factionService, FactionResearchModel research, IUnitRequestFactory requests,
            FactionRoster roster, StationLevelData levels) : base(registry, session)
        {
            _owner = owner;
            _economy = economy;
            _economyService = economyService;
            _faction = faction;
            _factionService = factionService;
            _research = research;
            _requests = requests;
            _roster = roster;
            _levels = levels;
        }

        public override string Key => $"player-{_owner.Id.Index}-economy";

        public override PlayerId Audience => _owner.Id;

        public override string Capture() => JsonUtility.ToJson(new Snapshot
        {
            Money = _economy.Money,
            IncomeTimeLeft = _economy.IncomeTimeLeft,
            Production = _faction.QueuedItems
                .Select(item => UnitRequestSave.From(item.UnitRequest, item.RemainingBuildTime)).ToArray(),
            Reservations = _faction.PurchaseReservations.Select(request => UnitRequestSave.From(request)).ToArray(),
            Research = ResearchSave.CaptureAll(_research)
        });

        public override void Apply(string json)
        {
            Snapshot snapshot = JsonUtility.FromJson<Snapshot>(json);
            _economy.Restore(snapshot.Money, snapshot.IncomeTimeLeft);
            ResearchSave.RestoreAll(snapshot.Research, _research);
            _faction.ReplaceQueue(snapshot.Production.Select(item =>
                new ProductionQueueItem(item.ToRequest(_requests, _roster, _levels), item.TimeLeft)),
                snapshot.Reservations.Select(item => item.ToRequest(_requests, _roster, _levels)));
        }

        protected override void FollowHost()
        {
            _economyService.FollowHost();
            _factionService.FollowHost();
        }

        [Serializable]
        private sealed class Snapshot
        {
            public float Money;
            public float IncomeTimeLeft;
            public UnitRequestSave[] Production;
            public UnitRequestSave[] Reservations;
            public ResearchSave[] Research;
        }
    }
}
