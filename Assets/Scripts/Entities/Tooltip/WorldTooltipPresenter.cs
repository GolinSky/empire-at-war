using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Tooltip;
using UnityEngine;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class WorldTooltipPresenter : IInitializable, ITickable, ILateDisposable, IObserver<BattleMap>
    {
        private readonly IHoveredEntityObserver _hover;
        private readonly IPointerInput _pointerInput;
        private readonly ITooltipService _tooltipService;
        private readonly IVisionService _visionService;
        private readonly ILocalPlayer _local;
        private readonly ICaptureSitesSystem _sites;
        private readonly ICameraService _cameraService;

        private readonly HardPointOverlayData _hardPointData;
        private readonly INotifier<BattleMap> _battleMap;
        private readonly List<IObstacleBoundsSource> _obstacles = new List<IObstacleBoundsSource>();

        private TooltipHandle _handle;

        private bool CanHover => _hover.CanHover;

        public WorldTooltipPresenter(IHoveredEntityObserver hover, IPointerInput pointerInput,
            ITooltipService tooltipService, IVisionService visionService, ILocalPlayer local,
            ICaptureSitesSystem sites, ICameraService cameraService, HardPointOverlayData hardPointData,
            INotifier<BattleMap> battleMap)
        {
            _hover = hover; _pointerInput = pointerInput; _tooltipService = tooltipService;
            _visionService = visionService; _local = local;
            _hardPointData = hardPointData; _sites = sites; _cameraService = cameraService;
            _battleMap = battleMap;
        }

        public void Initialize()
        { _battleMap.AddObserver(this); }

        public void LateDispose()
        {
            _battleMap.RemoveObserver(this);
            _tooltipService.Hide(_handle);
        }

        public void UpdateState(BattleMap battleMap) => _obstacles.AddRange(battleMap.Obstacles);

        public void Tick()
        {
            if (!CanHover) { _tooltipService.Hide(_handle); return; }
            Vector2 point = _pointerInput.Position;
            var anchor = new TooltipAnchor(TooltipAnchorKind.Cursor, point.x, point.y);
            if (_hover.TryGetHardPoint(out IEntity owner, out int hardPointId))
            {
                var key = (owner.Id, hardPointId);
                _handle = _tooltipService.Show(new TooltipContentProvider(this, key,
                    () => _hover.TryGetHardPoint(out IEntity current, out int id) &&
                        current.Id == owner.Id && id == hardPointId,
                    () => BuildHardPoint(owner, hardPointId)), anchor);
            }
            else if (_hover.TryGetHovered(out IEntity entity))
            {
                _handle = _tooltipService.Show(new TooltipContentProvider(this, entity.Id,
                    () => _hover.TryGetHovered(out IEntity current) && current.Id == entity.Id,
                    () => EntityTooltipContent.Build(entity)), anchor);
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
                    foreach (IObstacleBoundsSource obstacle in _obstacles)
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

        private TooltipContent BuildHardPoint(IEntity entity, int id)
        {
            IHardPointsFacade facade = entity.GetFacade<IHardPointsFacade>();
            IHardPointStatus hardPoint = facade.HardPoints[id];
            HardPointOverlayEntry entry = _hardPointData.Get(hardPoint.HardPointType);
            var stats = new List<TooltipStat>();
            string status = hardPoint.IsDestroyed ? "Destroyed" : "Operational";
            if (!hardPoint.TryGetWeaponProfile(out WeaponProfile weapon))
                return new TooltipContent(title: entry.DisplayName, description: entry.Description, stats: stats, status: status);

            stats.Add(new TooltipStat(label: "Salvo damage", current: weapon.Damage * weapon.ShotsPerSalvo));
            stats.Add(new TooltipStat(label: "Range", current: weapon.Range, format: "0"));
            return new TooltipContent(title: weapon.DisplayName, subtitle: entry.DisplayName, description: entry.Description,
                stats: stats, status: status);
        }
    }
}
