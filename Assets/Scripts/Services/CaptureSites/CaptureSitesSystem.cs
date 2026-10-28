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
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Ships;

namespace EmpireAtWar.Services.CaptureSites
{
    /// <summary>
    /// Owns the generated capture sites of a map: ship- and squadron-driven capture, paid construction
    /// through each side's <see cref="ISiteFacilityBuilder"/>, and reset when the facility dies.
    /// </summary>
    public sealed class CaptureSitesSystem : MonoBehaviour, ICaptureSitesSystem, IInitializable, ITickable,
        ILateDisposable, IUiCancelHandler, IObserver<BattleMap>, ICaptureSiteBuildSelection
    {
        private IShipService _shipService;
        private ISquadronRegistry _squadronRegistry;
        private IVisionService _visionService;
        private ICameraService _cameraService;
        private IPointerInput _pointerInput;
        private IPointerGestures _gestures;
        private IUiCancelRouter _cancelRouter;
        private IPlayerRegistry _playerRegistry;
        private IPlayerRoster _playerRoster;
        private ILocalPlayer _localPlayer;
        private ITooltipService _tooltipService;
        private INotifier<BattleMap> _battleMap;

        private readonly List<CaptureSitePresenter> _sites = new List<CaptureSitePresenter>();
        private CaptureStrengthSampler _strengthSampler;
        private CaptureSiteData _data;
        private CaptureSitePresenter _selectedSite;

        public IReadOnlyList<ICaptureSite> Sites => _sites;
        /// <summary>In map layout order, so a seeded rebuild matches saved sites by index.</summary>
        public IReadOnlyList<CaptureSitePresenter> SitePresenters => _sites;
        public event Action SelectionChanged = delegate { };
        public event Action<PlayerId, PlayerId, SiteFacilityType> SiteCaptured;
        public event Action<PlayerId, SiteFacilityType> SiteLost;
        public event Action<PlayerId, SiteFacilityType> ConstructionStarted;
        public CaptureSitePresenter SelectedSite => _selectedSite;
        public Vector2 SelectionScreenPosition { get; private set; }
        public bool CanAfford(float price) => GetBuilder(_localPlayer.Id).CanAfford(price);

        [Inject]
        private void Construct(
            IShipService shipService,
            ISquadronRegistry squadronRegistry,
            IVisionService visionService,
            ICameraService cameraService,
            IPointerInput pointerInput,
            IPointerGestures gestures,
            IUiCancelRouter cancelRouter,
            IPlayerRegistry playerRegistry,
            IPlayerRoster playerRoster,
            ILocalPlayer localPlayer,
            ITooltipService tooltipService,
            INotifier<BattleMap> battleMap,
            CaptureSiteData data)
        {
            _battleMap = battleMap;
            _tooltipService = tooltipService;
            _shipService = shipService;
            _squadronRegistry = squadronRegistry;
            _data = data;
            _visionService = visionService;
            _cameraService = cameraService;
            _pointerInput = pointerInput;
            _gestures = gestures;
            _cancelRouter = cancelRouter;
            _playerRegistry = playerRegistry;
            _playerRoster = playerRoster;
            _localPlayer = localPlayer;
            _strengthSampler = new CaptureStrengthSampler(shipService, squadronRegistry, playerRoster, data.SquadronCaptureWeight);
        }

        public void Initialize()
        {
            _gestures.WorldPressed += HandleWorldPressed;
            _gestures.WorldCommanded += HandleWorldPressed;
            _battleMap.AddObserver(this);
        }

        public void UpdateState(BattleMap battleMap)
        {
            foreach (CaptureSiteView view in battleMap.SiteViews)
            {
                CaptureSiteModel model = new CaptureSiteModel(
                    captureDuration: view.CaptureDuration, captureSpeedPerNetShip: _data.CaptureSpeedPerNetShip, relations: _playerRoster, startingOwner: view.StartingOwner,
                    facilityType: _data.GetAllowedFacilities(view.Kind)[0]);
                model.Captured += HandleSiteCaptured;
                model.Lost += HandleSiteLost;
                CaptureSitePresenter site = new CaptureSitePresenter(model: model, view: view, data: _data, localPlayer: _localPlayer,
                    tooltipService: _tooltipService, kind: view.Kind, pointId: view.PointId);
                _sites.Add(site);
                _strengthSampler.Add(position => site.Contains(position));
            }
        }

        public void LateDispose()
        {
            ClearSelection();
            _battleMap.RemoveObserver(this);
            _gestures.WorldPressed -= HandleWorldPressed;
            _gestures.WorldCommanded -= HandleWorldPressed;
            _cancelRouter.Unfocus(this);
            foreach (CaptureSitePresenter site in _sites)
            {
                site.Dispose();
            }

            _sites.Clear();
            _strengthSampler.Clear();
        }

        public void Tick()
        {
            float deltaTime = Time.deltaTime;
            _strengthSampler.Tick(deltaTime);

            for (int i = 0; i < _sites.Count; i++)
            {
                CaptureSitePresenter site = _sites[i];
                site.TickCapture(deltaTime, _strengthSampler[i]);
                if (site.TickConstruction(deltaTime))
                {
                    site.SetFacility(GetBuilder(site.Owner).Build(site.FacilityType, site.FacilityPosition,
                        site.ReleaseFacility));
                }

                bool isVisible = _visionService.IsVisible(_localPlayer.Id, site.Center);
                if (isVisible)
                {
                    // Out of vision the ring keeps its last seen owner.
                    site.Render();
                }

                bool isHovered = isVisible &&
                    site.Contains(_cameraService.GetWorldPoint(_pointerInput.Position, site.Center));
                site.SetVisibility(isVisible, isHovered);
            }
            if (_selectedSite != null && !IsSelectable(_selectedSite))
                ClearSelection();
        }

        private void HandleSiteCaptured(PlayerId previousOwner, PlayerId owner, SiteFacilityType type) =>
            SiteCaptured?.Invoke(previousOwner, owner, type);

        private void HandleSiteLost(PlayerId owner, SiteFacilityType type) => SiteLost?.Invoke(owner, type);

        public bool IsPositionInAnySite(Vector3 position, float clearance = 0f)
        {
            foreach (CaptureSitePresenter site in _sites)
            {
                if (site.Contains(position, clearance))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position)
        {
            return TryGetClosestSite(origin,
                site => site.IsCapturable && !_playerRoster.IsAllied(site.Owner, owner) &&
                    (site.Kind != CaptureSiteKind.Satellite || OwnsPointMines(site.PointId, owner)), out position);
        }

        private bool OwnsPointMines(int pointId, PlayerId owner)
        {
            bool hasMine = false;
            foreach (CaptureSitePresenter site in _sites)
            {
                if (site.Kind != CaptureSiteKind.Mining || site.PointId != pointId) continue;
                if (site.Owner != owner) return false;
                hasMine = true;
            }
            return hasMine;
        }

        public bool TryGetThreatenedSite(PlayerId owner, out Vector3 position)
        {
            foreach (CaptureSitePresenter site in _sites)
            {
                if (site.Owner == owner && HasHostileUnits(site, owner))
                {
                    position = GroundCenter(site);
                    return true;
                }
            }

            position = default;
            return false;
        }

        public bool TryGetRaidTarget(PlayerId attacker, Vector3 origin, out Vector3 position)
        {
            return TryGetClosestSite(origin,
                site => site.IsOperational && _playerRoster.IsHostile(attacker, site.Owner), out position);
        }

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

        private void HandleWorldPressed(Vector2 screenPosition)
        {
            ClearSelection();
            foreach (CaptureSitePresenter site in _sites)
            {
                if (IsSelectable(site) && site.Contains(_cameraService.GetWorldPoint(screenPosition, site.Center)))
                {
                    _selectedSite = site;
                    SelectionScreenPosition = screenPosition;
                    SelectionChanged.Invoke();
                    _cancelRouter.Focus(this);
                    return;
                }
            }
        }

        private void ClearSelection()
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

            return _squadronRegistry.HasSquadronInside(
                position => site.Contains(position),
                squadronOwner => _playerRoster.IsHostile(owner, squadronOwner));
        }

        private bool TryGetClosestSite(Vector3 origin, Predicate<CaptureSitePresenter> isTarget, out Vector3 position)
        {
            CaptureSitePresenter closestSite = null;
            float closestDistance = float.MaxValue;
            foreach (CaptureSitePresenter site in _sites)
            {
                float distance = (site.Center - origin).sqrMagnitude;
                if (distance < closestDistance && isTarget(site))
                {
                    closestDistance = distance;
                    closestSite = site;
                }
            }

            position = closestSite == null ? default : GroundCenter(closestSite);
            return closestSite != null;
        }

        private static Vector3 GroundCenter(CaptureSitePresenter site)
        {
            Vector3 center = site.Center;
            center.y = 0f;
            return center;
        }

        /// <summary>The local player may open the build menu only on a revealed site it can build on.</summary>
        private static bool IsSelectable(CaptureSitePresenter site) => site.CanPlayerBuild && site.IsRevealed;

        private ISiteFacilityBuilder GetBuilder(PlayerId owner)
        {
            return _playerRegistry.GetSiteBuilder(owner);
        }
    }
}
