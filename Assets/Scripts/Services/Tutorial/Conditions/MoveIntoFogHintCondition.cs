using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Services.Vision;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when the local player orders units to a point hidden by fog of war.</summary>
    public sealed class MoveIntoFogHintCondition : IHintCondition
    {
        private readonly IUnitOrderService _orders;
        private readonly ILocalPlayer _player;
        private readonly IVisionService _vision;

        public bool IsMet { get; private set; }

        public MoveIntoFogHintCondition(IUnitOrderService orders, ILocalPlayer player, IVisionService vision)
        {
            _orders = orders;
            _player = player;
            _vision = vision;
            _orders.OrderIssued += HandleOrderIssued;
        }

        private void HandleOrderIssued(UnitOrder order)
        {
            bool isMove = order.Action == UnitActionId.Move || order.Action == UnitActionId.AttackMove;
            if (isMove && _player.IsLocal(order.Issuer) && !_vision.IsVisible(_player.Id, order.Point)) IsMet = true;
        }

        public void Dispose() => _orders.OrderIssued -= HandleOrderIssued;
    }
}
