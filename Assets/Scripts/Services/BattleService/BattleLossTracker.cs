using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Saves;

namespace EmpireAtWar.Services.Battle
{
    public sealed class BattleLossTracker : IInitializable, ILateDisposable, IBattleSaveSource
    {
        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _localPlayer;
        private readonly SkirmishStateModel _state;
        private readonly Dictionary<IEntity, Action> _subscriptions = new();
        private readonly Dictionary<(BattleLossKind Kind, int TypeId, int StationLevel), int> _playerLosses = new();
        private readonly Dictionary<(BattleLossKind Kind, int TypeId, int StationLevel), int> _enemyLosses = new();
        // By owner for a client that joins mid-battle; empty for losses restored from a save.
        private readonly Dictionary<(PlayerId Owner, BattleLossKind Kind, int TypeId, int StationLevel), int> _ownerLosses =
            new();

        public BattleLossTracker(IEntityLocator entities, ILocalPlayer localPlayer, SkirmishStateModel state)
        {
            _entities = entities;
            _localPlayer = localPlayer;
            _state = state;
        }

        public void Initialize()
        {
            _entities.EntityAdded += Track;
            _entities.EntityRemoved += Untrack;
            foreach (IEntity entity in _entities.Entities)
                Track(entity);
        }

        public void LateDispose()
        {
            _entities.EntityAdded -= Track;
            _entities.EntityRemoved -= Untrack;
            foreach (var subscription in _subscriptions)
                subscription.Key.HealthModel.OnDestroy -= subscription.Value;
            _subscriptions.Clear();
        }

        public BattleUnitLoss[] GetPlayerLosses() => Snapshot(_playerLosses);

        public BattleUnitLoss[] GetEnemyLosses() => Snapshot(_enemyLosses);

        /// <summary>Host: every loss with its owner, so a joining client can sort them from its own side.</summary>
        public (PlayerId Owner, BattleUnitLoss Loss)[] GetLossesByOwner() =>
            _ownerLosses.Select(entry => (entry.Key.Owner,
                    new BattleUnitLoss(entry.Key.Kind, entry.Key.TypeId, entry.Value, entry.Key.StationLevel)))
                .ToArray();

        /// <summary>
        /// Client: the host's losses since the battle began. A count this client already recorded is kept when it
        /// is higher, since the host's copy can lag the deaths that reached this client.
        /// </summary>
        public void MergeHostLosses(IEnumerable<(PlayerId Owner, BattleUnitLoss Loss)> hostLosses)
        {
            var totals = new Dictionary<(bool Friendly, BattleLossKind Kind, int TypeId, int StationLevel), int>();
            foreach ((PlayerId owner, BattleUnitLoss loss) in hostLosses)
            {
                var key = (_localPlayer.IsFriendly(owner), loss.Kind, loss.TypeId, loss.StationLevel);
                totals.TryGetValue(key, out int count);
                totals[key] = count + loss.Count;
            }

            foreach (var total in totals)
            {
                var losses = total.Key.Friendly ? _playerLosses : _enemyLosses;
                var key = (total.Key.Kind, total.Key.TypeId, total.Key.StationLevel);
                losses.TryGetValue(key, out int count);
                losses[key] = Math.Max(count, total.Value);
            }
        }

        public void Reset()
        {
            _playerLosses.Clear();
            _enemyLosses.Clear();
            _ownerLosses.Clear();
        }

        public void Capture(BattleSave save)
        {
            save.PlayerLosses = GetPlayerLosses();
            save.EnemyLosses = GetEnemyLosses();
        }

        public void RestoreBeforeUnits(BattleSave save)
        {
            Reset();
            foreach (BattleUnitLoss loss in save.PlayerLosses)
                _playerLosses.Add((loss.Kind, loss.TypeId, loss.StationLevel), loss.Count);
            foreach (BattleUnitLoss loss in save.EnemyLosses)
                _enemyLosses.Add((loss.Kind, loss.TypeId, loss.StationLevel), loss.Count);
        }

        public void RestoreAfterUnits(BattleRestoreContext context)
        {
        }

        private void Track(IEntity entity)
        {
            if (entity.Owner.IsNone || entity.HealthModel.IsDestroyed) return;
            Action onDestroy;
            if (entity.TryGetFacade(out IUnitTypeFacade unit))
            {
                UnitTypeId type = unit.UnitTypeId;
                onDestroy = type.IsShip
                    ? () => Record(entity.Owner, BattleLossKind.Ship, (int)type.ShipType)
                    : () => Record(entity.Owner, BattleLossKind.Squadron, (int)type.SquadronType);
            }
            else if (entity.TryGetFacade(out IStructureSaveFacade structure))
            {
                onDestroy = structure.Kind switch
                {
                    StructureKind.SpaceStation or StructureKind.SecondStation =>
                        () => Record(entity.Owner, BattleLossKind.SpaceStation, structure.TypeId,
                            entity.GetFacade<IStationLevelFacade>().CurrentLevel),
                    StructureKind.DefendPlatform =>
                        () => Record(entity.Owner, BattleLossKind.DefendPlatform, structure.TypeId),
                    StructureKind.MiningFacility =>
                        () => Record(entity.Owner, BattleLossKind.MiningFacility, structure.TypeId),
                    _ => throw new ArgumentOutOfRangeException(nameof(structure.Kind), structure.Kind,
                        "Unknown battle structure.")
                };
            }
            else return;

            _subscriptions.Add(entity, onDestroy);
            entity.HealthModel.OnDestroy += onDestroy;
        }

        private void Untrack(IEntity entity)
        {
            if (_subscriptions.Remove(entity, out Action onDestroy))
                entity.HealthModel.OnDestroy -= onDestroy;
        }

        private void Record(PlayerId owner, BattleLossKind kind, int typeId, int stationLevel = 0)
        {
            if (_state.State != BattleState.Running && _state.State != BattleState.Paused) return;
            var losses = _localPlayer.IsFriendly(owner) ? _playerLosses : _enemyLosses;
            var key = (kind, typeId, stationLevel);
            losses.TryGetValue(key, out int count);
            losses[key] = count + 1;
            var ownerKey = (owner, kind, typeId, stationLevel);
            _ownerLosses.TryGetValue(ownerKey, out int ownerCount);
            _ownerLosses[ownerKey] = ownerCount + 1;
        }

        private static BattleUnitLoss[] Snapshot(
            Dictionary<(BattleLossKind Kind, int TypeId, int StationLevel), int> losses) =>
            losses.Select(entry => new BattleUnitLoss(entry.Key.Kind, entry.Key.TypeId, entry.Value, entry.Key.StationLevel))
                .ToArray();
    }
}
