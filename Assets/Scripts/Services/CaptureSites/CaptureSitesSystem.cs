using System;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Ship;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Services.CaptureSites
{
    /// <summary>
    /// Owns the generated capture sites of a map: ship- and squadron-driven capture, paid construction
    /// through each side's <see cref="ISiteFacilityBuilder"/>, and reset when the facility dies.
    /// </summary>
    public sealed class CaptureSitesSystem : MonoBehaviour, ICaptureSitesSystem, IInitializable, ITickable,
        ILateDisposable
    {
        // Explored fog retains 0.35 visibility; site status requires current vision.
        private const float MINIMUM_SITE_VISIBILITY = 0.5f;

        private readonly List<CaptureSitePresenter> _sites = new List<CaptureSitePresenter>();
        private readonly List<IEntity> _squadrons = new List<IEntity>();
        private CaptureSiteView[] _siteViews;
        private IShipService _shipService;
        private IEntityLocator _entityLocator;
        private CaptureSiteData _data;
        private FogOfWarSystem _fogOfWarSystem;
        private ICameraService _cameraService;
        private IInputService _inputService;
        private IPlayerRegistry _playerRegistry;
        private IPlayerRoster _roster;
        private ILocalPlayer _localPlayer;
        private CaptureTallyBuilder _tally;
        private CaptureSitePresenter _selectedSite;
        private Predicate<float> _canPlayerAfford;

        [Inject]
        private void Construct(
            IShipService shipService,
            IEntityLocator entityLocator,
            CaptureSiteData data,
            FogOfWarSystem fogOfWarSystem,
            ICameraService cameraService,
            IInputService inputService,
            IPlayerRegistry playerRegistry,
            IPlayerRoster roster,
            ILocalPlayer localPlayer,
            CaptureSiteView[] siteViews)
        {
            _siteViews = siteViews;
            _shipService = shipService;
            _entityLocator = entityLocator;
            _data = data;
            _fogOfWarSystem = fogOfWarSystem;
            _cameraService = cameraService;
            _inputService = inputService;
            _playerRegistry = playerRegistry;
            _roster = roster;
            _localPlayer = localPlayer;
            _tally = new CaptureTallyBuilder(roster);
        }

        public IReadOnlyList<CaptureSitePresenter> Sites => _sites;

        public void Initialize()
        {
            foreach (CaptureSiteView view in _siteViews)
            {
                CaptureSiteModel model = new CaptureSiteModel(
                    view.CaptureDuration, _data.CaptureSpeedPerNetShip, _roster);
                CaptureSitePresenter site = new CaptureSitePresenter(model, view, _data, _localPlayer);
                site.BuildRequested += HandleBuildRequested;
                _sites.Add(site);
            }

            _canPlayerAfford = price => GetBuilder(_localPlayer.Id).CanAfford(price);
            _inputService.OnInput += HandleInput;
            _inputService.OnEscapePressed += ClearSelection;
        }

        public void LateDispose()
        {
            _inputService.OnInput -= HandleInput;
            _inputService.OnEscapePressed -= ClearSelection;
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

            CollectSquadrons();
            foreach (CaptureSitePresenter site in _sites)
            {
                site.TickCapture(deltaTime, TallySite(site));
                if (site.TickConstruction(deltaTime))
                {
                    GetBuilder(site.Owner).Build(site.FacilityType, site.FacilityPosition, site.ReleaseFacility);
                }

                bool isVisible = !_fogOfWarSystem.IsHidden(site.Center, MINIMUM_SITE_VISIBILITY);
                if (isVisible)
                {
                    // Out of vision the ring keeps its last seen owner.
                    site.Render();
                }

                bool isHovered = isVisible && _inputService.SupportsHover &&
                    site.Contains(_cameraService.GetWorldPoint(_inputService.TouchPosition, site.Center));
                site.SetVisibility(isVisible, isHovered, _canPlayerAfford);
            }
        }

        public bool IsPositionInAnySite(Vector3 position, float clearance = 0f)
        {
            // Reads the views directly so zone layout can query sites before Initialize.
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
                if (!site.IsCapturable || _roster.IsAllied(site.Owner, owner))
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
                if (!site.IsOperational || !_roster.IsHostile(attacker, site.Owner))
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
                    return TryStartConstruction(site, ChooseFacilityType(owner));
                }
            }

            return false;
        }

        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 screenPosition)
        {
            // A world left-click (press) drops the selection; right-click (touch: tap release) picks a site.
            if (inputType == InputType.Selection && touchPhase == TouchPhase.Began)
            {
                ClearSelection();
                return;
            }

            if (inputType != InputType.ShipInput || touchPhase != TouchPhase.Ended)
            {
                return;
            }

            foreach (CaptureSitePresenter site in _sites)
            {
                if (site.CanPlayerBuild &&
                    site.Contains(_cameraService.GetWorldPoint(screenPosition, site.Center)))
                {
                    ClearSelection();
                    _selectedSite = site;
                    site.SetSelected(true);
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

            _selectedSite.SetSelected(false);
            _selectedSite = null;
        }

        private void HandleBuildRequested(CaptureSitePresenter site, SiteFacilityType facilityType)
        {
            if (_localPlayer.IsLocal(site.Owner) && site.CanStartConstruction)
            {
                TryStartConstruction(site, facilityType);
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

        private bool TryStartConstruction(CaptureSitePresenter site, SiteFacilityType facilityType)
        {
            if (!GetBuilder(site.Owner).TrySpend(site.GetCost(facilityType).Price))
            {
                return false;
            }

            if (site == _selectedSite)
            {
                _selectedSite = null;
            }

            site.StartConstruction(facilityType);
            return true;
        }

        private void CollectSquadrons()
        {
            _squadrons.Clear();
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (entity.Model is ISquadronModelObserver && !entity.HealthModel.IsDestroyed)
                {
                    _squadrons.Add(entity);
                }
            }
        }

        private CaptureTally TallySite(CaptureSitePresenter site)
        {
            _tally.Clear();
            _shipService.AddShipStrength(position => site.Contains(position), _tally);

            // A squadron is positioned at the centroid of its fighters.
            foreach (IEntity squadron in _squadrons)
            {
                if (site.Contains(squadron.GetFacade<IEntityTransformFacade>().Transform.position))
                {
                    _tally.Add(squadron.Owner, _data.SquadronCaptureWeight);
                }
            }

            return _tally.Build();
        }

        private bool HasHostileUnits(CaptureSitePresenter site, PlayerId owner)
        {
            foreach (IShipEntity ship in _shipService.Ships)
            {
                if (_roster.IsHostile(owner, ship.Owner) && site.Contains(ship.WorldPosition))
                {
                    return true;
                }
            }

            foreach (IEntity squadron in _squadrons)
            {
                if (_roster.IsHostile(owner, squadron.Owner) &&
                    site.Contains(squadron.GetFacade<IEntityTransformFacade>().Transform.position))
                {
                    return true;
                }
            }

            return false;
        }

        private ISiteFacilityBuilder GetBuilder(PlayerId owner)
        {
            return _playerRegistry.GetSiteBuilder(owner);
        }
    }
}
