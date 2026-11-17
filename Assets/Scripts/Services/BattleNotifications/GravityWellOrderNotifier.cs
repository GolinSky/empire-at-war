using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.UnitOrders;

namespace EmpireAtWar.Services.BattleNotifications
{
    /// <summary>Tells the local player why a selected ship ignored a movement order: its gravity well anchors it.</summary>
    public sealed class GravityWellOrderNotifier : IInitializable, ILateDisposable
    {
        private const string TEXT = "Gravity well active: ship cannot move";
        private const string GROUP_KEY = "gravity-well-anchored";

        private readonly IBattleNotifications _notifications;
        private readonly IUnitOrderService _orders;
        private readonly ISelectionService _selection;
        private readonly ILocalPlayer _local;

        public GravityWellOrderNotifier(IBattleNotifications notifications, IUnitOrderService orders,
            ISelectionService selection, ILocalPlayer local)
        {
            _notifications = notifications;
            _orders = orders;
            _selection = selection;
            _local = local;
        }

        public void Initialize() => _orders.OrderIssued += HandleOrder;

        public void LateDispose() => _orders.OrderIssued -= HandleOrder;

        private void HandleOrder(UnitOrder order)
        {
            if (!_local.IsLocal(order.Issuer) || !IsMovement(order.Action)) return;
            foreach (IEntity entity in _selection.PlayerSelectionContext.Entities)
            {
                if (!entity.TryGetFacade(out ICombatModifiersFacade modifiers) || !modifiers.Modifiers.IsAnchored)
                    continue;
                _notifications.Post(BattleNotificationPriority.Feedback, TEXT, GROUP_KEY);
                return;
            }
        }

        // Matches ShipOrderRunner's refused orders; Attack is accepted and Retreat ends the well.
        private static bool IsMovement(UnitActionId action) =>
            action == UnitActionId.Move || action == UnitActionId.WaypointMove || action == UnitActionId.AttackMove ||
            action == UnitActionId.Guard || action == UnitActionId.Hunt;
    }
}
