using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.UnitOrders;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when the local player sends a selected squadron somewhere (move or attack-move).</summary>
    public sealed class SquadronOrderedHintCondition : IHintCondition
    {
        private readonly IUnitOrderService _orders;
        private readonly ISelectionService _selection;
        private readonly ILocalPlayer _player;

        public bool IsMet { get; private set; }

        public SquadronOrderedHintCondition(IUnitOrderService orders, ISelectionService selection, ILocalPlayer player)
        {
            _orders = orders;
            _selection = selection;
            _player = player;
            _orders.OrderIssued += HandleOrderIssued;
        }

        private void HandleOrderIssued(UnitOrder order)
        {
            if (!_player.IsLocal(order.Issuer) ||
                order.Action != UnitActionId.Move && order.Action != UnitActionId.AttackMove &&
                order.Action != UnitActionId.WaypointMove) return;
            foreach (IEntity entity in _selection.PlayerSelectionContext.Entities)
                if (entity.TryGetFacade(out IUnitTypeFacade unit) && unit.UnitTypeId.IsSquadron) IsMet = true;
        }

        public void Dispose() => _orders.OrderIssued -= HandleOrderIssued;
    }
}
