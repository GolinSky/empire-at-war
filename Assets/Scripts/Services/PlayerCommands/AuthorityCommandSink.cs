using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.UnitOrders;

namespace EmpireAtWar.Services.PlayerCommands
{
    /// <summary>
    /// Runs commands on the machine that owns the simulation. Ids are resolved here; receivers not owned by the
    /// issuer are dropped, and per-player rules (money, cooldowns, site ownership) are checked by the handlers.
    /// </summary>
    public sealed class AuthorityCommandSink : IPlayerCommandSink
    {
        private readonly IEntityLocator _entities;
        private readonly IUnitOrderService _orders;
        private readonly PlayerCommandHandlers _handlers;

        public AuthorityCommandSink(IEntityLocator entities, IUnitOrderService orders, PlayerCommandHandlers handlers)
        {
            _entities = entities;
            _orders = orders;
            _handlers = handlers;
        }

        public void Submit(UnitOrderCommand command)
        {
            List<IEntity> receivers = OwnedEntities(command.ReceiverIds, command.Issuer);

            IEntity target = null;
            if (receivers.Count == 0 ||
                command.TargetId != UnitOrderCommand.NO_TARGET && !_entities.TryGetEntity(command.TargetId, out target))
                return;

            switch (command.Action)
            {
                case UnitActionId.Move:
                    _orders.IssueMove(receivers, command.Point);
                    break;
                case UnitActionId.AttackMove:
                    _orders.IssueAttackMove(receivers, command.Point);
                    break;
                case UnitActionId.Attack when command.TargetHardPointId != UnitOrderModel.NO_HARD_POINT:
                    _orders.IssueHardPointAttack(receivers, target, command.TargetHardPointId);
                    break;
                case UnitActionId.Attack:
                    _orders.IssueAttack(receivers, target);
                    break;
                case UnitActionId.Guard:
                    _orders.IssueGuard(receivers, target);
                    break;
                case UnitActionId.WaypointMove:
                    _orders.IssueWaypointMove(receivers, command.Waypoints);
                    break;
                case UnitActionId.Stop:
                    _orders.IssueStop(receivers);
                    break;
                case UnitActionId.Hunt:
                    _orders.IssueHunt(receivers);
                    break;
                case UnitActionId.Retreat:
                    _orders.IssueRetreat(receivers);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(command), command.Action, "Unknown unit action.");
            }
        }

        // Issuer validation: a player can only command its own units.
        private List<IEntity> OwnedEntities(long[] ids, PlayerId issuer)
        {
            List<IEntity> owned = new List<IEntity>(ids.Length);
            foreach (long id in ids)
            {
                if (_entities.TryGetEntity(id, out IEntity entity) && entity.Owner == issuer)
                    owned.Add(entity);
            }
            return owned;
        }

        public void Submit(PlaceMarkerCommand command) => _handlers.TacticalMarkers.TryPlace(command);

        public void Submit(PurchaseUnitCommand command) => _handlers.GetProduction(command.Issuer).Purchase(command);

        public void Submit(CancelPurchaseCommand command) => _handlers.GetProduction(command.Issuer).Cancel(command);

        public void Submit(BuildSiteFacilityCommand command) =>
            _handlers.SiteConstruction.TryStartAlliedConstruction(command.SiteIndex, command.Issuer, command.FacilityType);

        public void Submit(ShipAbilityCommand command)
        {
            List<IEntity> casters = OwnedEntities(command.CasterIds, command.Issuer);
            IEntity target = null;
            if (casters.Count == 0 ||
                command.TargetId != UnitOrderCommand.NO_TARGET && !_entities.TryGetEntity(command.TargetId, out target))
                return;
            _handlers.Abilities.Execute(casters, command.AbilityId, target, command.Cancel);
        }

        public void Submit(DeployReinforcementCommand command) =>
            _handlers.GetReinforcements(command.Issuer).Deploy(command);

        public void Submit(FireSuperWeaponCommand command)
        {
            if (_entities.TryGetEntity(command.TargetId, out IEntity target))
                _handlers.GetSuperWeapons(command.Issuer).Fire(command.Type, target);
        }
    }
}
