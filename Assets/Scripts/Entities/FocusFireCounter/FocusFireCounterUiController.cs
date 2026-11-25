using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.FocusFireCounter
{
    /// <summary>
    /// Shows the focus fire counter for the hovered hostile, else for the attack target of the player's selection.
    /// Counts are the local team's (<see cref="IFocusFireCounts"/>); targets without any cap show nothing.
    /// </summary>
    public sealed class FocusFireCounterUiController : UiController, IInitializable, ILateTickable
    {
        private readonly IHoveredEntityObserver _hover;
        private readonly ISelectionService _selection;
        private readonly ILocalPlayer _localPlayer;
        private readonly IPlayerRelations _relations;
        private readonly IFocusFireCounts _counts;
        private readonly IFocusFireCaps _caps;
        private readonly ICameraService _camera;
        private readonly IHudVisibilityObserver _hud;
        private FocusFireCounterUi _ui;

        public FocusFireCounterUiController(IUiService uiService, IHoveredEntityObserver hover,
            ISelectionService selection, ILocalPlayer localPlayer, IPlayerRelations relations, IFocusFireCounts counts,
            IFocusFireCaps caps, ICameraService camera, IHudVisibilityObserver hud) : base(uiService)
        {
            _hover = hover;
            _selection = selection;
            _localPlayer = localPlayer;
            _relations = relations;
            _counts = counts;
            _caps = caps;
            _camera = camera;
            _hud = hud;
        }

        public void Initialize()
        {
            _ui = CreateUi<FocusFireCounterUi>();
            _ui.Initialize();
        }

        public void LateTick()
        {
            IEntity target = _hud.IsHudVisible ? FindTarget() : null;
            if (target == null)
            {
                _ui.HideCounts();
                return;
            }

            int shipCap = _caps.GetCap(target, isSquadron: false);
            int squadronCap = _caps.GetCap(target, isSquadron: true);
            Vector3 world = target.GetFacade<IEntityTransformFacade>().Transform.position;
            if (shipCap == FocusFireCaps.UNLIMITED && squadronCap == FocusFireCaps.UNLIMITED ||
                _camera.WorldToViewportPoint(world).z <= 0f)
            {
                _ui.HideCounts();
                return;
            }

            _ui.ShowCounts(_camera.WorldToScreenPoint(world),
                _counts.CountHolders(target, isSquadron: false), shipCap,
                _counts.CountHolders(target, isSquadron: true), squadronCap);
        }

        private IEntity FindTarget()
        {
            if (_hover.TryGetHovered(out IEntity hovered) && IsShownTarget(hovered)) return hovered;
            ISelectionContext selection = _selection.PlayerSelectionContext;
            for (int i = 0; i < selection.Entities.Count; i++)
            {
                if (!selection.Entities[i].TryGetFacade(out IUnitOrderObserverFacade orders)) continue;
                IEntity target = orders.AttackTarget;
                if (target != null && IsShownTarget(target)) return target;
            }

            return null;
        }

        private bool IsShownTarget(IEntity entity) =>
            !entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits && !entity.IsCloaked() &&
            _relations.IsHostile(_localPlayer.Id, entity.Owner);
    }
}
