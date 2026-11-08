using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Ship;
using global::VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.VContainer
{
    [LifecycleOrder(Initialize = LifecycleOrders.PLAYER_SCOPES_INITIALIZE,
        Dispose = LifecycleOrders.PLAYER_SCOPES_DISPOSE)]
    public sealed class PlayerScopeBootstrap : IInitializable, IOrderedDisposable
    {
        private readonly SkirmishScope _owner;
        private readonly IPlayerRoster _roster;
        private readonly PlayerFactoryLookup _factoryLookup;
        private readonly IMatchSession _session;
        private readonly List<AssetLifetimeScope> _players = new List<AssetLifetimeScope>();
        private readonly List<(PlayerId Host, PlayerId Ally, HostedFactionLevel Level)> _hostedLevels =
            new List<(PlayerId Host, PlayerId Ally, HostedFactionLevel Level)>();

        public PlayerScopeBootstrap(SkirmishScope owner, IPlayerRoster roster, PlayerFactoryLookup factoryLookup,
            IMatchSession session)
        {
            _owner = owner;
            _roster = roster;
            _factoryLookup = factoryLookup;
            _session = session;
        }

        public void Initialize()
        {
            try
            {
                BuildPlayers();
            }
            catch (Exception exception)
            {
                var failures = new CleanupFailures();
                failures.Add(exception);
                ReleasePlayers(failures);
                failures.ThrowIfAny();
            }
        }

        private void BuildPlayers()
        {
            foreach (PlayerSlot slot in _roster.Players)
            {
                // AI runs only on the authority; a client mirrors AI slots as remote players.
                BattlePlayer ai = slot.IsAi && _session.IsAuthority ? _owner.Battle.GetPlayer(slot.Id) : null;
                _players.Add(BuildScope(slot, ai));
            }

            foreach (BattlePlayer player in _owner.Battle.Players)
            {
                if (!player.IsHosted || !_session.IsAuthority) continue;
                PlayerId host = _owner.Battle.GetProductionHost(player).Slot.Id;
                LinkHostedLevel(host, player.Slot.Id);
            }
        }

        /// <summary>
        /// Online host: a human left for good. Their scope (and every unit and structure in it) is disposed and an
        /// AI scope takes the slot; <see cref="EmpireAtWar.Services.Networking.AiTakeoverService"/> restores the
        /// player's units and state into it. An AI ally hosted by this player follows the new AI's station level.
        /// </summary>
        public void ReplaceWithAi(BattlePlayer ai)
        {
            if (!_session.IsAuthority) throw new InvalidOperationException("Only the host runs AI players.");
            PlayerId id = ai.Slot.Id;
            List<PlayerId> hostedAllies = new List<PlayerId>();
            for (int index = _hostedLevels.Count - 1; index >= 0; index--)
            {
                if (_hostedLevels[index].Host != id) continue;
                hostedAllies.Add(_hostedLevels[index].Ally);
                _hostedLevels[index].Level.Dispose();
                _hostedLevels.RemoveAt(index);
            }

            _factoryLookup.Unregister(id);
            var failures = new CleanupFailures();
            failures.DisposeScope(_players[id.Index]);
            failures.ThrowIfAny();
            _players[id.Index] = BuildScope(ai.Slot, ai);
            foreach (PlayerId ally in hostedAllies) LinkHostedLevel(id, ally);
        }

        private AssetLifetimeScope BuildScope(PlayerSlot slot, BattlePlayer ai)
        {
            GameObject playerRoot = new GameObject($"Player{slot.Id.Index}");
            playerRoot.SetActive(false);
            playerRoot.transform.SetParent(_owner.transform, false);
            AssetLifetimeScope player;
            if (ai != null)
            {
                AiPlayerScope aiScope = playerRoot.AddComponent<AiPlayerScope>();
                aiScope.ConfigurePlayer(ai);
                player = aiScope;
            }
            else
            {
                HumanPlayerScope human = playerRoot.AddComponent<HumanPlayerScope>();
                human.ConfigurePlayer(slot, slot.Id == _session.LocalPlayerId);
                player = human;
            }

            player.autoRun = false;
            player.parentReference.Object = _owner;
            try
            {
                player.Build();
                _factoryLookup.Register(slot.Id, new PlayerUnitFactories(
                    player.Container.Resolve<ShipFactory>(), player.Container.Resolve<ISquadronSpawner>(),
                    player.Container.Resolve<SpaceStationFactory>(), player.Container.Resolve<MiningFacilityFactory>(),
                    player.Container.Resolve<DefendPlatformFactory>()));
            }
            catch (Exception exception)
            {
                var failures = new CleanupFailures();
                failures.Add(exception);
                failures.DisposeScope(player);
                failures.ThrowIfAny();
            }

            playerRoot.SetActive(true);
            return player;
        }

        private void LinkHostedLevel(PlayerId host, PlayerId ally) =>
            _hostedLevels.Add((host, ally, new HostedFactionLevel(
                _players[host.Index].Container.Resolve<IFactionLevelObserver>(),
                _players[ally.Index].Container.Resolve<EnemyFactionModel>())));

        public void Dispose()
        {
            var failures = new CleanupFailures();
            ReleasePlayers(failures);
            failures.ThrowIfAny();
        }

        private void ReleasePlayers(CleanupFailures failures)
        {
            for (int index = _hostedLevels.Count - 1; index >= 0; index--)
                failures.Attempt(_hostedLevels[index].Level.Dispose);
            _hostedLevels.Clear();
            _factoryLookup.Clear();
            for (int index = _players.Count - 1; index >= 0; index--)
                failures.DisposeScope(_players[index]);
            _players.Clear();
        }
    }
}
