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
        private readonly List<AssetLifetimeScope> _players = new List<AssetLifetimeScope>();
        private readonly List<HostedFactionLevel> _hostedLevels = new List<HostedFactionLevel>();

        public PlayerScopeBootstrap(SkirmishScope owner, IPlayerRoster roster, PlayerFactoryLookup factoryLookup)
        {
            _owner = owner;
            _roster = roster;
            _factoryLookup = factoryLookup;
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
                GameObject playerRoot = new GameObject($"Player{slot.Id.Index}");
                playerRoot.SetActive(false);
                playerRoot.transform.SetParent(_owner.transform, false);
                AssetLifetimeScope player;
                if (slot.IsAi)
                {
                    AiPlayerScope ai = playerRoot.AddComponent<AiPlayerScope>();
                    ai.ConfigurePlayer(slot);
                    player = ai;
                }
                else
                {
                    LocalPlayerScope local = playerRoot.AddComponent<LocalPlayerScope>();
                    local.ConfigurePlayer(slot);
                    player = local;
                }

                player.autoRun = false;
                player.parentReference.Object = _owner;
                _players.Add(player);
                player.Build();
                _factoryLookup.Register(slot.Id, new PlayerUnitFactories(
                    player.Container.Resolve<ShipFactory>(), player.Container.Resolve<ISquadronSpawner>(),
                    player.Container.Resolve<SpaceStationFactory>(), player.Container.Resolve<MiningFacilityFactory>(),
                    player.Container.Resolve<DefendPlatformFactory>()));
                playerRoot.SetActive(true);
            }

            foreach (BattlePlayer player in _owner.Battle.Players)
            {
                if (!player.IsHosted) continue;
                PlayerId host = _owner.Battle.GetProductionHost(player).Slot.Id;
                _hostedLevels.Add(new HostedFactionLevel(
                    _players[host.Index].Container.Resolve<PlayerFactionModel>(),
                    _players[player.Slot.Id.Index].Container.Resolve<EnemyFactionModel>()));
            }
        }

        public void Dispose()
        {
            var failures = new CleanupFailures();
            ReleasePlayers(failures);
            failures.ThrowIfAny();
        }

        private void ReleasePlayers(CleanupFailures failures)
        {
            for (int index = _hostedLevels.Count - 1; index >= 0; index--)
                failures.Attempt(_hostedLevels[index].Dispose);
            _hostedLevels.Clear();
            _factoryLookup.Clear();
            for (int index = _players.Count - 1; index >= 0; index--)
                failures.DisposeScope(_players[index]);
            _players.Clear();
        }
    }
}
