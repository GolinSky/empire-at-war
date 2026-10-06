using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map;
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
using EmpireAtWar.Services.Vision;
using Zenject;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class WorldTooltipPresenter : IInitializable, ITickable, ILateDisposable, IObserver<BattleMap>
    {
        private readonly ISelectionQuery _query;
        private readonly IPointerInput _pointerInput;
        private readonly IUiHitTest _ui;
        private readonly IPointerGestures _gestures;
        private readonly ITooltipService _tooltipService;
        private readonly IVisionService _visionService;
        private readonly ILocalPlayer _local;
        private readonly IHudVisibilityObserver _hud;
        private readonly IHardPointHoverObserver _hardPointHover;
        private readonly ICaptureSitesSystem _sites;
        private readonly ICameraService _cameraService;

        private readonly HardPointOverlayData _hardPointData;
        private readonly WeaponsData _weaponsData;
        private readonly INotifier<BattleMap> _battleMap;
        private readonly List<IMiniMapObstacleSource> _obstacles = new List<IMiniMapObstacleSource>();

        private TooltipHandle _handle;

        private bool _dragging;

        private bool CanHover => _hud.IsHudVisible && !_dragging && !_ui.IsOverUi(_pointerInput.Position);

        public WorldTooltipPresenter(ISelectionQuery query, IPointerInput pointerInput, IUiHitTest ui,
            IPointerGestures gestures, ITooltipService tooltipService, IVisionService visionService,
            ILocalPlayer local, IHudVisibilityObserver hud, IHardPointHoverObserver hardPointHover,
            ICaptureSitesSystem sites, ICameraService cameraService, HardPointOverlayData hardPointData,
            WeaponsData weaponsData, INotifier<BattleMap> battleMap)
        {
            _query = query; _pointerInput = pointerInput; _ui = ui; _gestures = gestures; _tooltipService = tooltipService;
            _visionService = visionService; _local = local; _hud = hud;
            _hardPointHover = hardPointHover; _hardPointData = hardPointData; _weaponsData = weaponsData; _sites = sites; _cameraService = cameraService;
            _battleMap = battleMap;
        }

        public void Initialize()
        { _gestures.DragStarted += StartDrag; _gestures.DragEnded += EndDrag; _battleMap.AddObserver(this); }

        public void LateDispose()
        {
            _gestures.DragStarted -= StartDrag; _gestures.DragEnded -= EndDrag; _battleMap.RemoveObserver(this);
            _tooltipService.Hide(_handle);
        }

        public void UpdateState(BattleMap battleMap) => _obstacles.AddRange(battleMap.Obstacles);

        private void StartDrag(Vector2 point) { _dragging = true; _tooltipService.Hide(_handle); }

        private void EndDrag(Vector2 point) => _dragging = false;

        public void Tick()
        {
            if (!CanHover) { _tooltipService.Hide(_handle); return; }
            Vector2 point = _pointerInput.Position;
            var anchor = new TooltipAnchor(TooltipAnchorKind.Cursor, point.x, point.y);
            if (_hardPointHover.TryGetHovered(out IEntity owner, out int hardPointId) && IsVisible(owner))
            {
                var key = (owner.Id, hardPointId);
                _handle = _tooltipService.Show(new TooltipContentProvider(this, key,
                    () => CanHover && IsVisible(owner) && _hardPointHover.TryGetHovered(out IEntity current, out int id) &&
                        current.Id == owner.Id && id == hardPointId,
                    () => BuildHardPoint(owner, hardPointId)), anchor);
            }
            else if (_query.TryFindAt(point, out SelectionEntry selection) && IsVisible(selection.Entity))
            {
                IEntity entity = selection.Entity;
                _handle = _tooltipService.Show(new TooltipContentProvider(this, entity.Id,
                    () => CanHover && IsVisible(entity), () => EntityTooltipContent.Build(entity)), anchor);
            }
            else
            {
                Vector3 world = _cameraService.GetWorldPoint(point, Vector3.zero);
                foreach (ICaptureSite site in _sites.Sites)
                {
                    if (!site.IsRevealed || !site.Contains(world)) continue;
                    _handle = _tooltipService.Show(new TooltipContentProvider(this, site,
                        () => CanHover && site.IsRevealed, () => new TooltipContent(title: "Capture site",
                            description: "Move ships into the ring to capture it. Select an owned empty site to construct a facility.",
                            stats: new[] { new TooltipStat(label: "Capture (%)", current: site.CaptureProgress * 100f),
                                new TooltipStat(label: "Construction (%)", current: site.ConstructionProgress * 100f) },
                            status: $"Owner: {site.Owner} · {site.State} · {site.FacilityType}")), anchor);
                    return;
                }
                RaycastHit hit = _cameraService.ScreenPointToRay(point);
                if (hit.collider != null && _visionService.IsVisible(_local.Id, hit.point))
                    foreach (IMiniMapObstacleSource obstacle in _obstacles)
                    {
                        if (!obstacle.WorldBounds.Contains(hit.point)) continue;
                        _handle = _tooltipService.Show(new TooltipContentProvider(this, obstacle,
                            () => CanHover && _visionService.IsVisible(_local.Id, hit.point),
                            () => new TooltipContent(title: "Obstacle", description: "Blocks movement and prevents move orders into its occupied area.")), anchor);
                        return;
                    }
                _tooltipService.Hide(_handle);
            }
        }

        private bool IsVisible(IEntity entity) => !entity.HealthModel.IsDestroyed &&
            (_local.IsFriendly(entity.Owner) || _visionService.IsEntityVisible(_local.Id, entity));

        private TooltipContent BuildHardPoint(IEntity entity, int id)
        {
            IHardPointsFacade facade = entity.GetFacade<IHardPointsFacade>();
            IHardPointStatus hardPoint = facade.HardPoints[id];
            HardPointOverlayEntry entry = _hardPointData.Get(hardPoint.HardPointType);
            var stats = new List<TooltipStat> { new TooltipStat(label: "Component health", current: hardPoint.Health, max: hardPoint.MaxHealth) };
            if (facade.MaxShields > 0f) stats.Add(new TooltipStat(label: "Shared ship shields", current: entity.HealthModel.Shields, max: facade.MaxShields));
            string status = hardPoint.IsDestroyed ? "Destroyed" : "Operational";
            if (!hardPoint.TryGetWeaponType(out WeaponType weaponType))
                return new TooltipContent(title: entry.DisplayName, description: entry.Description, stats: stats, status: status);

            WeaponProfile weapon = _weaponsData.GetProfile(weaponType);
            stats.Add(new TooltipStat(label: "Salvo damage", current: weapon.Damage * weapon.ShotsPerSalvo));
            stats.Add(new TooltipStat(label: "Range", current: weapon.Range, format: "0"));
            return new TooltipContent(title: weapon.DisplayName, subtitle: entry.DisplayName, description: entry.Description,
                stats: stats, status: status);
        }
    }
}
