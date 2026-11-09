using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.UnitOrders;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class OrderIssuedHintCondition : IHintCondition
    {
        private readonly IUnitOrderService _orders;
        private readonly ILocalPlayer _player;
        private readonly UnitActionId[] _actions;

        public bool IsMet { get; private set; }

        public OrderIssuedHintCondition(IUnitOrderService orders, ILocalPlayer player, params UnitActionId[] actions)
        {
            _orders = orders;
            _player = player;
            _actions = actions;
            _orders.OrderIssued += HandleOrderIssued;
        }

        private void HandleOrderIssued(UnitOrder order)
        {
            if (System.Array.IndexOf(_actions, order.Action) >= 0 && _player.IsLocal(order.Issuer)) IsMet = true;
        }

        public void Dispose() => _orders.OrderIssued -= HandleOrderIssued;
    }
}
