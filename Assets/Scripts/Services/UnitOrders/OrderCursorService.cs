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

            if (!_abilities.IsWaitingForTarget && _superWeapons.Pending == null &&
                _targeting.Pending == UnitActionId.AttackMove)
                return _uiHitTest.IsOverUi(_pointer.Position) || _orderInput.CanIssueAttackMove(_pointer.Position)
                    ? OrderCursor.None : OrderCursor.Invalid;

            if (!_hover.TryGetHovered(out IEntity hovered) || !IsEnemy(hovered)) return OrderCursor.None;
            // Clicking an ineligible enemy keeps ability targeting armed, so the cursor marks it as invalid.
            if (_abilities.IsWaitingForTarget)
                return _abilities.IsValidTarget(hovered) ? OrderCursor.None : OrderCursor.Invalid;
            if (_superWeapons.Pending != null) return OrderCursor.None;
            UnitActionId? pending = _targeting.Pending;
            if (pending != null && pending != UnitActionId.Attack) return OrderCursor.None;
            return HasAttacker() ? OrderCursor.Attack : OrderCursor.None;
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
