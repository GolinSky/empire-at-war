using EmpireAtWar.Entities.Capture;
using EmpireAtWar.Services.Tooltip;
using System;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Ship;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Services.SpawnBlocking;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Ships;
using Unity.Profiling;
using EmpireAtWar.Utils;

namespace EmpireAtWar.Services.CaptureSites
{
    /// <summary>
    /// Owns the generated capture sites of a map: ship- and squadron-driven capture, paid construction
    /// through each side's <see cref="ISiteFacilityBuilder"/>, and reset when the facility dies.
    /// </summary>
    public sealed class CaptureSitesSystem : MonoBehaviour, ICaptureSiteDirectory, ICaptureSiteEvents,
        ICaptureSitePresence, ICaptureSiteConstruction, ICaptureSiteSaveState, IInitializable, ITickable,
        ILateDisposable, IUiCancelHandler, IObserver<BattleMap>, ICaptureSiteBuildSelection
    {
        private static readonly ProfilerMarker CaptureMarker = new ProfilerMarker("Battle.Capture.Site.Model");
        private static readonly ProfilerMarker RenderMarker = new ProfilerMarker("Battle.Capture.Site.Render");
        private static readonly ProfilerMarker VisibilityMarker = new ProfilerMarker("Battle.Capture.Site.Visibility");
        private readonly Dictionary<ICaptureSite, Func<Vector3, bool>> _siteAreas = new Dictionary<ICaptureSite, Func<Vector3, bool>>();
        private readonly Dictionary<PlayerId, Predicate<PlayerId>> _hostilityFilters = new Dictionary<PlayerId, Predicate<PlayerId>>();
        private IShipService _shipService;
        private ISquadronRegistry _squadronRegistry;
        private IVisionService _visionService;
        private ICameraService _cameraService;
        private IPointerGestures _gestures;
        private IUiCancelRouter _cancelRouter;
        private IPlayerRegistry _playerRegistry;
        private IPlayerRoster _playerRoster;
        private ILocalPlayer _localPlayer;
        private ITooltipService _tooltipService;
        private INotifier<BattleMap> _battleMap;

        private readonly List<CaptureSitePresenter> _sites = new List<CaptureSitePresenter>();
        private readonly List<Transform> _areaSources = new List<Transform>();
        private IBattleAreaWriter _areaWriter;
        private CaptureStrengthSampler _strengthSampler;
        private CaptureSiteData _data;
        private CaptureSitePresenter _selectedSite;

        public IReadOnlyList<ICaptureSite> Sites => _sites;
        /// <summary>In map layout order, so a seeded rebuild matches saved sites by index.</summary>
        public IReadOnlyList<CaptureSitePresenter> SitePresenters => _sites;
        public event Action SitesChanged;
        public event Action SelectionChanged = delegate { };
        public event Action<PlayerId, PlayerId, SiteFacilityType> SiteCaptured;
        public event Action<PlayerId, SiteFacilityType> SiteLost;
        public event Action<PlayerId, SiteFacilityType> ConstructionStarted;
        public event Action<PlayerId, SiteFacilityType> ConstructionCompleted;
        public CaptureSitePresenter SelectedSite => _selectedSite;
        public Vector2 SelectionScreenPosition => _cameraService.WorldToScreenPoint(_selectedSite.FacilityPosition);
        public bool CanAfford(float price) => GetBuilder(_localPlayer.Id).CanAfford(price);

        [Inject]
        private void Construct(
            IShipService shipService,
            ISquadronRegistry squadronRegistry,
            IVisionService visionService,
            ICameraService cameraService,
            IPointerGestures gestures,
            IUiCancelRouter cancelRouter,
            IPlayerRegistry playerRegistry,
            IPlayerRoster playerRoster,
            ILocalPlayer localPlayer,
            ITooltipService tooltipService,
            INotifier<BattleMap> battleMap,
            CaptureSiteData data,
            IBattleAreaWriter areaWriter)
        {
            _areaWriter = areaWriter;
            _battleMap = battleMap;
            _tooltipService = tooltipService;
            _shipService = shipService;
            _squadronRegistry = squadronRegistry;
            _data = data;
            _visionService = visionService;
            _cameraService = cameraService;
            _gestures = gestures;
            _cancelRouter = cancelRouter;
            _playerRegistry = playerRegistry;
            _playerRoster = playerRoster;
            _localPlayer = localPlayer;
            _strengthSampler = new CaptureStrengthSampler(shipService, squadronRegistry, playerRoster, data.SquadronCaptureWeight);
        }

        public void Initialize()
        {
            _gestures.WorldClicked += HandleWorldClicked;
            _battleMap.AddObserver(this);
        }

        public void UpdateState(BattleMap battleMap)
        {
            ClearSites();
            foreach (CaptureSiteView view in battleMap.SiteViews)
            {
                CaptureSiteModel model = new CaptureSiteModel(
                    captureDuration: view.CaptureDuration, captureSpeedPerNetShip: _data.CaptureSpeedPerNetShip, relations: _playerRoster, startingOwner: view.StartingOwner,
                    facilityType: _data.GetAllowedFacilities(view.Kind)[0]);
                CaptureSitePresenter site = new CaptureSitePresenter(model: model, view: view, data: _data, localPlayer: _localPlayer,
                    tooltipService: _tooltipService, kind: view.Kind, pointId: view.PointId);
                _sites.Add(site);
                site.Captured += HandleSiteCaptured;
                site.Lost += HandleSiteLost;
                Transform source = view.transform;
                _areaSources.Add(source);
                _areaWriter.Register(BattleAreaLayer.StructureExclusion, PlayerId.None, source, site.Radius);
                _areaWriter.Register(BattleAreaLayer.OrderSnap, PlayerId.None, source, site.Radius);
                site.ConstructionCompleted += HandleConstructionCompleted;
                Vector3 center = default;
                float radius = 0f;
                _strengthSampler.Add(position => PlanarGeometry.ContainsCircle(position, center, radius), () =>
                {
                    center = site.Center;
                    radius = site.Radius;
                });
            }
            SitesChanged?.Invoke();
        }

        public void LateDispose()
        {
            ClearSites();
            _battleMap.RemoveObserver(this);
            _gestures.WorldClicked -= HandleWorldClicked;
            _cancelRouter.Unfocus(this);
        }

        private void ClearSites()
        {
            ClearSelection();
            foreach (Transform source in _areaSources)
            {
                _areaWriter.Unregister(BattleAreaLayer.StructureExclusion, source);
                _areaWriter.Unregister(BattleAreaLayer.OrderSnap, source);
            }
            _areaSources.Clear();
            foreach (CaptureSitePresenter site in _sites)
            {
                site.Captured -= HandleSiteCaptured;
                site.Lost -= HandleSiteLost;
                site.ConstructionCompleted -= HandleConstructionCompleted;
                site.Dispose();
            }

            _sites.Clear();
            _siteAreas.Clear();
            _hostilityFilters.Clear();
            _strengthSampler.Clear();
        }

        public void Tick()
        {
            float deltaTime = Time.deltaTime;
            _strengthSampler.Tick(deltaTime);

            for (int i = 0; i < _sites.Count; i++)
            {
                CaptureSitePresenter site = _sites[i];
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (CaptureMarker.Auto())
#endif
                site.TickCapture(deltaTime, _strengthSampler[i]);

                bool isVisible;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (VisibilityMarker.Auto())
#endif
                isVisible = _visionService.IsVisible(_localPlayer.Id, site.Center);
                if (isVisible)
                {
                    // Out of vision the ring keeps its last seen owner.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    using (RenderMarker.Auto())
#endif
                    site.Render();
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (VisibilityMarker.Auto())
#endif
                site.SetVisibility(isVisible);
            }
            if (_selectedSite != null && !IsSelectable(_selectedSite))
                ClearSelection();
        }

        private void HandleSiteCaptured(PlayerId previousOwner, PlayerId owner, SiteFacilityType type) =>
            SiteCaptured?.Invoke(previousOwner, owner, type);

        private void HandleSiteLost(PlayerId owner, SiteFacilityType type) => SiteLost?.Invoke(owner, type);

        private void HandleConstructionCompleted(PlayerId owner, SiteFacilityType type) =>
            ConstructionCompleted?.Invoke(owner, type);

        public bool TryBuildOnOwnedSite(PlayerId owner)
        {
            foreach (CaptureSitePresenter site in _sites)
            {
                // Paying for a site that hostile units are about to take would waste the credits.
                if (site.Kind != CaptureSiteKind.Satellite && site.Owner == owner && site.CanStartConstruction && !HasHostileUnits(site, owner))
                {
                    // A mine pays for itself in seconds; a Battle Asteroid costs more and gives up most of the income.
                    return TryStartConstruction(site, SiteFacilityType.Mining, owner);
                }
            }

            return false;
        }

        public bool TryCancel()
        {
            if (_selectedSite == null)
            {
                return false;
            }

            ClearSelection();
            return true;
        }

        private void HandleWorldClicked(Vector2 screenPosition)
        {
            ClearSelection();
            Ray ray = _cameraService.SceneCamera.ScreenPointToRay(screenPosition);
            CaptureSitePresenter site = CaptureSitePointerQuery.Find(_sites, ray);
            if (site == null || !IsSelectable(site)) return;

            _selectedSite = site;
            SelectionChanged.Invoke();
            _cancelRouter.Focus(this);
        }

        public void ClearSelection()
        {
            if (_selectedSite == null)
            {
                return;
            }

            _selectedSite = null;
            _cancelRouter.Unfocus(this);
            SelectionChanged.Invoke();
        }

        public void BuildSelected(SiteFacilityType facilityType)
        {
            if (_selectedSite != null && IsSelectable(_selectedSite))
            {
                TryStartConstruction(_selectedSite, facilityType, _localPlayer.Id);
            }
        }

        public bool TryStartConstruction(int siteIndex, PlayerId owner, SiteFacilityType type)
        {
            CaptureSitePresenter site = _sites[siteIndex];
            return site.Owner == owner && !HasHostileUnits(site, owner) && TryStartConstruction(site, type, owner);
        }

        private bool TryStartConstruction(CaptureSitePresenter site, SiteFacilityType facilityType, PlayerId payer)
        {
            if (!site.CanStartConstruction || !_playerRoster.IsAllied(site.Owner, payer) ||
                !_data.CanBuild(site.Kind, facilityType) || !GetBuilder(payer).TrySpend(site.GetCost(facilityType).Price))
            {
                return false;
            }

            if (site == _selectedSite)
            {
                ClearSelection();
            }

            site.StartConstruction(facilityType);
            ConstructionStarted?.Invoke(payer, facilityType);
            site.SetFacility(GetBuilder(site.Owner).Build(facilityType, site.FacilityPosition,
                site.ReleaseFacility, site.SetConstructionProgress));
            return true;
        }

        public bool HasHostileUnits(ICaptureSite site, PlayerId owner)
        {
            foreach (IShipEntity ship in _shipService.Ships)
            {
                if (_playerRoster.IsHostile(owner, ship.Owner) && site.Contains(ship.WorldPosition))
                {
                    return true;
                }
            }

            if (!_siteAreas.TryGetValue(site, out Func<Vector3, bool> contains))
            {
                contains = CreateSiteArea(site);
                _siteAreas.Add(site, contains);
            }
            if (!_hostilityFilters.TryGetValue(owner, out Predicate<PlayerId> isHostile))
            {
                isHostile = CreateHostilityFilter(owner);
                _hostilityFilters.Add(owner, isHostile);
            }
            return _squadronRegistry.HasSquadronInside(contains, isHostile);
        }

        private static Func<Vector3, bool> CreateSiteArea(ICaptureSite site) => position => site.Contains(position);

        private Predicate<PlayerId> CreateHostilityFilter(PlayerId owner) =>
            squadronOwner => _playerRoster.IsHostile(owner, squadronOwner);

        /// <summary>The local player may open the build menu only on a revealed site it can build on.</summary>
        private static bool IsSelectable(CaptureSitePresenter site) => site.CanPlayerBuild && site.IsRevealed;

        private ISiteFacilityBuilder GetBuilder(PlayerId owner)
        {
            return _playerRegistry.GetSiteBuilder(owner);
        }
    }
}
