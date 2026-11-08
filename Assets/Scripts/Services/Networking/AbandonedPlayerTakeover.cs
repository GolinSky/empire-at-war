using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.VContainer;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host: a human stayed disconnected past the grace period, so an AI takes the slot. The player's units,
    /// structures and economy are captured like a save, the slot is rebuilt as an AI scope and everything is
    /// restored under the same ids — clients see no change (<see cref="NetworkTickService.BeginHandover"/>).
    /// The human's build queue and reserve become the AI's pending builds.
    /// </summary>
    public sealed class AbandonedPlayerTakeover : IInitializable, ILateDisposable
    {
        private readonly OnlineMatchService _online;
        private readonly BattleDefinition _battle;
        private readonly BattleSaveService _saves;
        private readonly UnitSaveService _units;
        private readonly PlayerSaveRegistry _players;
        private readonly PlayerScopeBootstrap _scopes;
        private readonly NetworkTickService _ticks;
        private readonly IEntityLocator _entities;
        private readonly IReadOnlyList<ICaptureSiteSaveState> _captureSites;

        public AbandonedPlayerTakeover(OnlineMatchService online, BattleDefinition battle, BattleSaveService saves,
            UnitSaveService units, PlayerSaveRegistry players, PlayerScopeBootstrap scopes, NetworkTickService ticks,
            IEntityLocator entities, IReadOnlyList<ICaptureSiteSaveState> captureSites)
        {
            _online = online;
            _battle = battle;
            _saves = saves;
            _units = units;
            _players = players;
            _scopes = scopes;
            _ticks = ticks;
            _entities = entities;
            _captureSites = captureSites;
        }

        public void Initialize() => _online.PlayerAbandoned += TakeOver;

        public void LateDispose() => _online.PlayerAbandoned -= TakeOver;

        private void TakeOver(PlayerId player)
        {
            // An ended battle has nothing left to play.
            if (!_saves.CanSave)
            {
                Debug.Log($"[Online] Player {player} left; the battle is not running, so no AI takes over.");
                return;
            }

            BattleSave battle = _saves.Capture(BattleSaveSlot.Autosave);
            BattleSave save = BattleSave.Create(battle.Header, battle.Setup, battle.Camera,
                battle.Units.Where(unit => unit.Owner == player).ToArray(),
                battle.Structures.Where(structure => structure.Owner == player).ToArray());
            save.CaptureSites = battle.CaptureSites;
            save.Players = new[] { ToAiSave(battle.Players.Single(saved => saved.Player == player)) };
            HashSet<long> rebuilt = new HashSet<long>(save.Units.Select(unit => unit.Id)
                .Concat(save.Structures.Select(structure => structure.Id)));
            // Other units chasing or guarding the rebuilt ones hold the old entities; they get their order again.
            List<UnitSave> retargeted = battle.Units
                .Where(unit => unit.Owner != player && unit.Order.TargetsAny(rebuilt)).ToList();

            _ticks.BeginHandover();
            try
            {
                _scopes.ReplaceWithAi(CreateAiPlayer(_battle.GetPlayer(player)));
                _players.RestoreBeforeUnits(save);
                BattleRestoreContext context = _units.Restore(save);
                _players.RestoreAfterUnits(context);
                foreach (ICaptureSiteSaveState sites in _captureSites) RelinkSiteFacilities(sites, context);
                var attackMoveGroups = new Dictionary<string, AttackMoveEngagement>();
                foreach (UnitSave unit in retargeted)
                {
                    if (_entities.TryGetEntity(unit.Id, out IEntity entity))
                        unit.Order.Apply(entity, _entities, attackMoveGroups);
                }
            }
            finally
            {
                _ticks.EndHandover();
            }

            Debug.Log($"[Online] Player {player} did not reconnect within {OnlineMatchService.RECONNECT_GRACE_SECONDS}s; an AI took over.");
        }

        private static BattlePlayer CreateAiPlayer(BattlePlayer human)
        {
            PlayerSlot slot = human.Slot;
            return new BattlePlayer(new PlayerSlot(slot.Id, slot.Team, slot.Faction, PlayerController.Ai,
                slot.Difficulty, slot.ColorIndex), PlayerRole.AiOpponent, human.Features);
        }

        // Queued (paid) builds and built units waiting in reserve become AI builds; reserve ones finish at once.
        private static PlayerSave ToAiSave(PlayerSave human)
        {
            PlayerSave ai = PlayerSave.Create(human.Player);
            if (human.HasMoney) ai.SetMoney(human.Money, human.IncomeTimeLeft);
            ai.StationLevel = human.StationLevel;
            ai.Research = human.Research;
            ai.PendingBuilds = human.Production.Concat(human.Reserve).ToArray();
            ai.PaidSquadrons = human.PaidSquadrons;
            ai.SuperWeapons = human.SuperWeapons;
            return ai;
        }

        // Sites keep their state; a rebuilt facility must free its site again when it is destroyed.
        private static void RelinkSiteFacilities(ICaptureSiteSaveState sites, BattleRestoreContext context)
        {
            foreach (CaptureSitePresenter site in sites.SitePresenters)
            {
                if (!site.HasFacility) continue;
                if (context.MiningFacilities.TryGetValue(site.FacilityId, out var facility))
                {
                    facility.OnRelease += site.ReleaseFacility;
                    facility.ConstructionProgressChanged += site.SetConstructionProgress;
                }
                else if (context.DefendPlatforms.TryGetValue(site.FacilityId, out var platform))
                {
                    platform.OnRelease += site.ReleaseFacility;
                    platform.ConstructionProgressChanged += site.SetConstructionProgress;
                }
            }
        }
    }
}
