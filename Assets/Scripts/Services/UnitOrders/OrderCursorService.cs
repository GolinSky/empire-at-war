using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Services.UnitOrders
{
    /// <summary>Single owner of the order cursor: mirrors what <see cref="PlayerOrderInputHandler"/> does on a world click.</summary>
    public sealed class OrderCursorService : IOrderCursorService
    {
        private readonly IPlayerOrderInputHandler _orderInput;
        private readonly IPointerInput _pointer;
        private readonly IUiHitTest _uiHitTest;
        private readonly IHoveredEntityObserver _hover;
        private readonly ILocalPlayer _localPlayer;
        private readonly IPlayerRegistry _players;
        private readonly ISelectionService _selectionService;
        private readonly IShipAbilityTargeting _abilities;
        private readonly SuperWeaponTargetingModel _superWeapons;
        private readonly UnitActionTargetingModel _targeting;

        public OrderCursorService(IHoveredEntityObserver hover, ILocalPlayer localPlayer,
            ISelectionService selectionService, IShipAbilityTargeting abilities,
            SuperWeaponTargetingModel superWeapons, UnitActionTargetingModel targeting, IPlayerRegistry players,
            IPlayerOrderInputHandler orderInput, IPointerInput pointer, IUiHitTest uiHitTest)
        {
            _orderInput = orderInput;
            _pointer = pointer;
            _uiHitTest = uiHitTest;
            _hover = hover;
            _localPlayer = localPlayer;
            _players = players;
            _selectionService = selectionService;
            _abilities = abilities;
            _superWeapons = superWeapons;
            _targeting = targeting;
        }

        public OrderCursor Resolve()
        {
            if (_players.TryGetReinforcementReserve(_localPlayer.Id, out ReinforcementModel reserve) &&
                reserve.IsTrySpawning)
                return reserve.CanDeployPreview ? OrderCursor.None : OrderCursor.Invalid;

            bool hasHovered = _hover.TryGetHovered(out IEntity hovered);
            UnitActionId? pending = _targeting.Pending;
            bool isArmed = _superWeapons.Pending != null || _abilities.IsWaitingForTarget || pending != null;
            if (isArmed && _uiHitTest.IsOverUi(_pointer.Position)) return OrderCursor.None;

            // Mirrors the click: Invalid marks a unit a click would refuse while the mode stays armed.
            if (_superWeapons.Pending != null) return OrderCursor.SuperWeapon;
            if (_abilities.IsWaitingForTarget)
                return hasHovered && IsEnemy(hovered) && !_abilities.IsValidTarget(hovered)
                    ? OrderCursor.Invalid : OrderCursor.AbilityTarget;
            switch (pending)
            {
                case UnitActionId.AttackMove:
                    return _orderInput.CanIssueAttackMove(_pointer.Position) ? OrderCursor.AttackMove : OrderCursor.Invalid;
                case UnitActionId.Attack:
                    return OrderCursor.Attack;
                case UnitActionId.Guard:
                    return hasHovered && _localPlayer.IsFriendly(hovered.Owner) &&
                           !hovered.TryGetFacade(out IEntitySelectionFacade _)
                        ? OrderCursor.Invalid : OrderCursor.Guard;
                case UnitActionId.WaypointMove:
                    return OrderCursor.Waypoint;
                case null:
                    return hasHovered && IsEnemy(hovered) && HasAttacker() ? OrderCursor.Attack : OrderCursor.None;
                default:
                    return OrderCursor.None;
            }
        }

        private bool HasAttacker()
        {
            foreach (IEntity entity in _selectionService.PlayerSelectionContext.Entities)
            {
                if (entity.HealthModel.IsDestroyed || !entity.HealthModel.HasUnits) continue;
                if (entity.TryGetFacade(out IAttackFacade _) ||
                    entity.TryGetFacade(out IHardPointAttackFacade _) ||
                    entity.TryGetFacade(out IFocusFireFacade _))
                    return true;
            }
            return false;
        }

        private bool IsEnemy(IEntity entity) =>
            _localPlayer.IsHostile(entity.Owner) && entity.HealthModel.HasUnits;
    }
}
