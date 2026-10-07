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
using Zenject;

namespace EmpireAtWar.Services.CaptureSites
{
    /// <summary>
    /// Owns the generated capture sites of a map: ship- and squadron-driven capture, paid construction
    /// through each side's <see cref="ISiteFacilityBuilder"/>, and reset when the facility dies.
    /// </summary>
    public sealed class CaptureSitesSystem : MonoBehaviour, ICaptureSitesSystem, IInitializable, ITickable,
        ILateDisposable, IUiCancelHandler, IObserver<BattleMap>
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
        private IUiService _uiService;
        private INotifier<BattleMap> _battleMap;

        private readonly List<CaptureSitePresenter> _sites = new List<CaptureSitePresenter>();
        private IReadOnlyList<CaptureSiteView> _siteViews;
        private CaptureSiteData _data;
        private CaptureStrengthBuilder _captureStrengthBuilder;
        private CaptureSitePresenter _selectedSite;
        private Predicate<float> _canPlayerAfford;

        public IReadOnlyList<ICaptureSite> Sites => _sites;

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
            IUiService uiService,
            INotifier<BattleMap> battleMap,
            CaptureSiteData data)
        {
            _battleMap = battleMap;
            _tooltipService = tooltipService;
            _uiService = uiService;
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
            _captureStrengthBuilder = new CaptureStrengthBuilder(playerRoster);
        }

        public void Initialize()
        {
            _canPlayerAfford = price => GetBuilder(_localPlayer.Id).CanAfford(price);
            _gestures.WorldPressed += HandleWorldPressed;
            _gestures.WorldCommanded += HandleWorldPressed;
            _battleMap.AddObserver(this);
        }

        public void UpdateState(BattleMap battleMap)
        {
            _siteViews = battleMap.SiteViews;
            foreach (CaptureSiteView view in _siteViews)
            {
                view.InitializeBuildUi(_uiService.DynamicCanvasTransform);
                CaptureSiteModel model = new CaptureSiteModel(
                    captureDuration: view.CaptureDuration, captureSpeedPerNetShip: _data.CaptureSpeedPerNetShip, relations: _playerRoster);
                CaptureSitePresenter site = new CaptureSitePresenter(model: model, view: view, data: _data, localPlayer: _localPlayer,
                    tooltipService: _tooltipService, canAfford: price => GetBuilder(_localPlayer.Id).CanAfford(price));
                site.BuildRequested += HandleBuildRequested;
                _sites.Add(site);
            }
        }

        public void LateDispose()
        {
            _battleMap.RemoveObserver(this);
            _gestures.WorldPressed -= HandleWorldPressed;
            _gestures.WorldCommanded -= HandleWorldPressed;
            _cancelRouter.Unfocus(this);
            foreach (CaptureSitePresenter site in _sites)
            {
                site.BuildRequested -= HandleBuildRequested;
                site.Dispose();
            }

            _sites.Clear();
        }

        public void Tick()
        {
            float deltaTime = Time.deltaTime;
            if (_selectedSite != null && !_selectedSite.CanPlayerBuild)
            {
                ClearSelection();
            }

            foreach (CaptureSitePresenter site in _sites)
            {
                site.TickCapture(deltaTime, CalculateCaptureStrength(site));
                if (site.TickConstruction(deltaTime))
                {
                    GetBuilder(site.Owner).Build(site.FacilityType, site.FacilityPosition, site.ReleaseFacility);
                }

                bool isVisible = _visionService.IsVisible(_localPlayer.Id, site.Center);
                if (isVisible)
                {
                    // Out of vision the ring keeps its last seen owner.
                    site.Render();
                }

                bool isHovered = isVisible &&
                    site.Contains(_cameraService.GetWorldPoint(_pointerInput.Position, site.Center));
                site.SetVisibility(isVisible, isHovered, _canPlayerAfford);
            }
        }

        public bool IsPositionInAnySite(Vector3 position, float clearance = 0f)
        {
            foreach (CaptureSiteView view in _siteViews)
            {
                float x = position.x - view.Center.x;
                float z = position.z - view.Center.z;
                float radius = view.Radius + clearance;
                if (x * x + z * z <= radius * radius)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position)
        {
            CaptureSitePresenter closestSite = null;
            float closestDistance = float.MaxValue;
            foreach (CaptureSitePresenter site in _sites)
            {
                if (!site.IsCapturable || _playerRoster.IsAllied(site.Owner, owner))
                {
                    continue;
                }

                float distance = (site.Center - origin).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestSite = site;
                }
            }

            if (closestSite == null)
            {
                position = default;
                return false;
            }

            position = closestSite.Center;
            position.y = 0f;
            return true;
        }

        public bool TryGetThreatenedSite(PlayerId owner, out Vector3 position)
        {
            foreach (CaptureSitePresenter site in _sites)
            {
                if (site.Owner == owner && HasHostileUnits(site, owner))
                {
                    position = site.Center;
                    position.y = 0f;
                    return true;
                }
            }

            position = default;
            return false;
        }

        public bool TryGetRaidTarget(PlayerId attacker, Vector3 origin, out Vector3 position)
        {
            CaptureSitePresenter closestSite = null;
            float closestDistance = float.MaxValue;
            foreach (CaptureSitePresenter site in _sites)
            {
                if (!site.IsOperational || !_playerRoster.IsHostile(attacker, site.Owner))
                {
                    continue;
                }

                float distance = (site.Center - origin).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestSite = site;
                }
            }

            if (closestSite == null)
            {
                position = default;
                return false;
            }

            position = closestSite.Center;
            position.y = 0f;
            return true;
        }

        public bool TryBuildOnOwnedSite(PlayerId owner)
        {
            foreach (CaptureSitePresenter site in _sites)
            {
                // Paying for a site that hostile units are about to take would waste the credits.
                if (site.Owner == owner && site.CanStartConstruction && !HasHostileUnits(site, owner))
                {
                    return TryStartConstruction(site, ChooseFacilityType(owner), owner);
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
                if (site.CanPlayerBuild &&
                    site.Contains(_cameraService.GetWorldPoint(screenPosition, site.Center)))
                {
                    _selectedSite = site;
                    site.Select(screenPosition);
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

            _selectedSite.Deselect();
            _selectedSite = null;
            _cancelRouter.Unfocus(this);
        }

        private void HandleBuildRequested(CaptureSitePresenter site, SiteFacilityType facilityType)
        {
            if (site.CanPlayerBuild)
            {
                TryStartConstruction(site, facilityType, _localPlayer.Id);
            }
        }

        /// <summary>Keeps a side's facilities balanced: builds whichever type it owns fewer of, mining first.</summary>
        private SiteFacilityType ChooseFacilityType(PlayerId owner)
        {
            int miningCount = 0;
            int battleAsteroidCount = 0;
            foreach (CaptureSitePresenter site in _sites)
            {
                if (site.Owner != owner || !site.HasFacility)
                {
                    continue;
                }

                if (site.FacilityType == SiteFacilityType.Mining)
                {
                    miningCount++;
                }
                else
                {
                    battleAsteroidCount++;
                }
            }

            return battleAsteroidCount < miningCount ? SiteFacilityType.BattleAsteroid : SiteFacilityType.Mining;
        }

        private bool TryStartConstruction(CaptureSitePresenter site, SiteFacilityType facilityType, PlayerId payer)
        {
            if (!GetBuilder(payer).TrySpend(site.GetCost(facilityType).Price))
            {
                return false;
            }

            if (site == _selectedSite)
            {
                _selectedSite = null;
                _cancelRouter.Unfocus(this);
            }

            site.StartConstruction(facilityType);
            return true;
        }

        private CaptureStrength CalculateCaptureStrength(CaptureSitePresenter site)
        {
            Func<Vector3, bool> contains = position => site.Contains(position);
            _captureStrengthBuilder.Clear();
            _shipService.AddShipStrength(contains, _captureStrengthBuilder);
            _squadronRegistry.AddSquadronStrength(contains, _data.SquadronCaptureWeight, _captureStrengthBuilder);
            return _captureStrengthBuilder.Build();
        }

        private bool HasHostileUnits(CaptureSitePresenter site, PlayerId owner)
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

        private ISiteFacilityBuilder GetBuilder(PlayerId owner)
        {
            return _playerRegistry.GetSiteBuilder(owner);
        }
    }
}
