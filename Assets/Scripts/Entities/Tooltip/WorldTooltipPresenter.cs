using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class WorldTooltipPresenter : IInitializable, ITickable, ILateDisposable
    {
        private readonly ISelectionQuery _query;
        private readonly IPointerInput _pointer;
        private readonly IUiHitTest _ui;
        private readonly IPointerGestures _gestures;
        private readonly ITooltipService _tooltips;
        private readonly FactionsData _factions;
        private readonly IFogOfWarSystem _fog;
        private readonly ILocalPlayer _local;
        private readonly IHudVisibilityObserver _hud;
        private readonly IHardPointHoverObserver _hardPointHover;
        private readonly HardPointOverlayData _hardPointData;
        private readonly ICaptureSitesSystem _sites;
        private readonly ICameraService _camera;
        private readonly List<IMiniMapObstacleSource> _obstacles;
        private TooltipHandle _handle;
        private bool _dragging;

        public WorldTooltipPresenter(ISelectionQuery query, IPointerInput pointer, IUiHitTest ui,
            IPointerGestures gestures, ITooltipService tooltips, FactionsData factions, IFogOfWarSystem fog,
            ILocalPlayer local, IHudVisibilityObserver hud, IHardPointHoverObserver hardPointHover,
            HardPointOverlayData hardPointData, ICaptureSitesSystem sites, ICameraService camera,
            List<IMiniMapObstacleSource> obstacles)
        {
            _query = query; _pointer = pointer; _ui = ui; _gestures = gestures; _tooltips = tooltips;
            _factions = factions; _fog = fog; _local = local; _hud = hud;
            _hardPointHover = hardPointHover; _hardPointData = hardPointData; _sites = sites; _camera = camera;
            _obstacles = obstacles;
        }

        public void Initialize()
        { _gestures.DragStarted += StartDrag; _gestures.DragEnded += EndDrag; }
        public void LateDispose()
        { _gestures.DragStarted -= StartDrag; _gestures.DragEnded -= EndDrag; _tooltips.Hide(_handle); }
        private void StartDrag(Vector2 point) { _dragging = true; _tooltips.Hide(_handle); }
        private void EndDrag(Vector2 point) => _dragging = false;
        private bool CanHover => _hud.IsHudVisible && !_dragging && !_ui.IsOverUi(_pointer.Position);

        public void Tick()
        {
            if (!CanHover) { _tooltips.Hide(_handle); return; }
            Vector2 point = _pointer.Position;
            var anchor = new TooltipAnchor(TooltipAnchorKind.Cursor, point.x, point.y);
            if (_hardPointHover.TryGetHovered(out IEntity owner, out int hardPointId) && IsVisible(owner))
            {
                var key = (owner.Id, hardPointId);
                _handle = _tooltips.Show(new TooltipContentProvider(this, key,
                    () => CanHover && IsVisible(owner) && _hardPointHover.TryGetHovered(out IEntity current, out int id) &&
                        current.Id == owner.Id && id == hardPointId,
                    () => BuildHardPoint(owner, hardPointId)), anchor);
            }
            else if (_query.TryFindAt(point, out SelectionEntry selection) && IsVisible(selection.Entity))
            {
                IEntity entity = selection.Entity;
                _handle = _tooltips.Show(new TooltipContentProvider(this, entity.Id,
                    () => CanHover && IsVisible(entity), () => EntityTooltipContent.Build(entity, _factions)), anchor);
            }
            else
            {
                Vector3 world = _camera.GetWorldPoint(point, Vector3.zero);
                foreach (ICaptureSite site in _sites.Sites)
                {
                    if (!site.IsRevealed || !site.Contains(world)) continue;
                    _handle = _tooltips.Show(new TooltipContentProvider(this, site,
                        () => CanHover && site.IsRevealed, () => new TooltipContent("Capture site",
                            "Move ships into the ring to capture it. Select an owned empty site to construct a facility.",
                            stats: new[] { new TooltipStat("Capture (%)", site.CaptureProgress * 100f),
                                new TooltipStat("Construction (%)", site.ConstructionProgress * 100f) },
                            status: $"Owner: {site.Owner} · {site.State} · {site.FacilityType}")), anchor);
                    return;
                }
                RaycastHit hit = _camera.ScreenPointToRay(point);
                if (hit.collider != null && !_fog.IsHidden(hit.point))
                    foreach (IMiniMapObstacleSource obstacle in _obstacles)
                    {
                        if (!obstacle.WorldBounds.Contains(hit.point)) continue;
                        _handle = _tooltips.Show(new TooltipContentProvider(this, obstacle,
                            () => CanHover && !_fog.IsHidden(hit.point),
                            () => new TooltipContent("Obstacle", "Blocks movement and prevents move orders into its occupied area.")), anchor);
                        return;
                    }
                _tooltips.Hide(_handle);
            }
        }

        private bool IsVisible(IEntity entity) => !entity.HealthModel.IsDestroyed &&
            (_local.IsFriendly(entity.Owner) || !_fog.IsHidden(entity.GetFacade<IEntityTransformFacade>().Transform.position));

        private TooltipContent BuildHardPoint(IEntity entity, int id)
        {
            IHardPointsFacade facade = entity.GetFacade<IHardPointsFacade>();
            IHardPointStatus hardPoint = facade.HardPoints[id];
            HardPointOverlayEntry entry = _hardPointData.Get(hardPoint.HardPointType);
            var stats = new List<TooltipStat> { new TooltipStat("Component health", hardPoint.Health, hardPoint.MaxHealth) };
            if (facade.MaxShields > 0f) stats.Add(new TooltipStat("Shared ship shields", entity.HealthModel.Shields, facade.MaxShields));
            return new TooltipContent(entry.DisplayName, entry.Description,
                stats: stats, status: hardPoint.IsDestroyed ? "Destroyed" : "Operational");
        }
    }
}
