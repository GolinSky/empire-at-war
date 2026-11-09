using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.Reinforcement;

namespace EmpireAtWar.Services.BattleNotifications
{
    /// <summary>
    /// Local production cards: research complete, unit ready in reserve, unit deployed, fleet capacity full and own
    /// station level with what it unlocked. Online clients receive the same feedback through <see cref="ShowReplicated"/>.
    /// </summary>
    public sealed class EconomyNotifier : IInitializable, ILateDisposable
    {
        private const int MAX_LISTED_UNLOCKS = 3;

        private readonly IBattleNotifications _notifications;
        private readonly FactionService _faction;
        private readonly ReinforcementService _reinforcements;
        private readonly PlayerFactionModel _model;
        private readonly FactionResearchModel _research;
        private readonly FactionRoster _roster;
        private readonly List<string> _unlocks = new List<string>();

        public EconomyNotifier(IBattleNotifications notifications, FactionService faction,
            ReinforcementService reinforcements, PlayerFactionModel model, FactionResearchModel research,
            FactionRoster roster)
        {
            _notifications = notifications;
            _faction = faction;
            _reinforcements = reinforcements;
            _model = model;
            _research = research;
            _roster = roster;
        }

        public void Initialize()
        {
            _faction.ResearchCompleted += HandleResearchCompleted;
            _faction.UnitProduced += HandleUnitProduced;
            _faction.StationUpgraded += HandleStationUpgraded;
            _reinforcements.ReinforcementDeployed += HandleDeployed;
            _reinforcements.CapacityRejected += HandleCapacityRejected;
        }

        public void LateDispose()
        {
            _faction.ResearchCompleted -= HandleResearchCompleted;
            _faction.UnitProduced -= HandleUnitProduced;
            _faction.StationUpgraded -= HandleStationUpgraded;
            _reinforcements.ReinforcementDeployed -= HandleDeployed;
            _reinforcements.CapacityRejected -= HandleCapacityRejected;
        }

        /// <summary>Owner-only live feedback from the host; restoring replicated state never calls this path.</summary>
        public void ShowReplicated(FactionFeedbackKind kind, UnitRequest request, int stationLevel)
        {
            switch (kind)
            {
                case FactionFeedbackKind.ResearchCompleted:
                    _notifications.Post(BattleNotificationPriority.Progress, "Research complete", "research",
                        "{0} research projects complete");
                    break;
                case FactionFeedbackKind.UnitProduced: HandleUnitProduced(request); break;
                case FactionFeedbackKind.StationUpgraded: PostStationLevel(stationLevel); break;
                case FactionFeedbackKind.ReinforcementDeployed: HandleDeployed(request); break;
                case FactionFeedbackKind.CapacityRejected: HandleCapacityRejected(); break;
                case FactionFeedbackKind.PurchaseAccepted:
                case FactionFeedbackKind.PurchaseCanceled:
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown faction feedback.");
            }
        }

        private void HandleResearchCompleted(ResearchType type)
        {
            int tier = _research.GetCompletedTiers(type);
            string name = _roster.ResearchLines[type].Tiers[tier - 1].FactionData.Name;
            _notifications.Post(BattleNotificationPriority.Progress, $"{name} complete", "research",
                "{0} research projects complete");
        }

        private void HandleUnitProduced(UnitRequest request)
        {
            if (!IsUnit(request)) return;
            _notifications.Post(BattleNotificationPriority.Progress, $"{request.FactionData.Name} ready in reserve",
                "unit.ready", "{0} units ready in reserve");
        }

        private void HandleDeployed(UnitRequest request)
        {
            if (!IsUnit(request)) return;
            _notifications.Post(BattleNotificationPriority.Progress, $"{request.FactionData.Name} deployed",
                "unit.deployed", "{0} units deployed");
        }

        private void HandleCapacityRejected() =>
            _notifications.Post(BattleNotificationPriority.Feedback, "Fleet capacity full", "capacity");

        private void HandleStationUpgraded() => PostStationLevel(_model.CurrentLevel);

        private void PostStationLevel(int level)
        {
            CollectUnlocks(level);
            if (_unlocks.Count == 0)
            {
                _notifications.Post(BattleNotificationPriority.Tactical, $"Station level {level}");
                return;
            }
            int listed = Math.Min(MAX_LISTED_UNLOCKS, _unlocks.Count);
            string items = string.Join(", ", _unlocks.GetRange(0, listed));
            if (_unlocks.Count > listed) items += $" +{_unlocks.Count - listed} more";
            _notifications.Post(BattleNotificationPriority.Tactical, $"Station level {level} — unlocked {items}");
        }

        private void CollectUnlocks(int level)
        {
            _unlocks.Clear();
            AddUnlocks(_roster.Ships.Values, level);
            AddUnlocks(_roster.Squadrons.Values, level);
            AddUnlocks(_roster.MiningFacilities.Values, level);
            AddUnlocks(_roster.DefendPlatforms.Values, level);
            AddUnlocks(_roster.SuperWeapons.Values, level);
        }

        private void AddUnlocks(IEnumerable<FactionData> units, int level)
        {
            foreach (FactionData data in units)
                if (data.AvailableLevel == level) _unlocks.Add(data.Name);
        }

        private static bool IsUnit(UnitRequest request) => request is ShipUnitRequest || request is SquadronUnitRequest;
    }
}
