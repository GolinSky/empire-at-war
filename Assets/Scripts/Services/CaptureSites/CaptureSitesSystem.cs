using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
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
        private LazyInject<ISiteFacilityBuilder> _playerBuilder;
        private LazyInject<ISiteFacilityBuilder> _opponentBuilder;
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
            [Inject(Id = PlayerType.Player)] LazyInject<ISiteFacilityBuilder> playerBuilder,
            [Inject(Id = PlayerType.Opponent)] LazyInject<ISiteFacilityBuilder> opponentBuilder,
            CaptureSiteView[] siteViews)
        {
            _siteViews = siteViews;
            _shipService = shipService;
            _entityLocator = entityLocator;
            _data = data;
            _fogOfWarSystem = fogOfWarSystem;
            _cameraService = cameraService;
            _inputService = inputService;
            _playerBuilder = playerBuilder;
            _opponentBuilder = opponentBuilder;
        }

        public IReadOnlyList<CaptureSitePresenter> Sites => _sites;

        public void Initialize()
        {
            foreach (CaptureSiteView view in _siteViews)
            {
                CaptureSiteModel model = new CaptureSiteModel(view.CaptureDuration, _data.CaptureSpeedPerNetShip);
                CaptureSitePresenter site = new CaptureSitePresenter(model, view, _data);
                site.BuildRequested += HandleBuildRequested;
                _sites.Add(site);
            }

            _canPlayerAfford = price => _playerBuilder.Value.CanAfford(price);
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
                GetStrength(site, out float playerStrength, out float opponentStrength);
                site.TickCapture(deltaTime, playerStrength, opponentStrength);
                if (site.TickConstruction(deltaTime))
                {
                    GetBuilder(site.Owner).Build(site.FacilityType, site.Center, site.ReleaseFacility);
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

        public bool TryGetCaptureTarget(PlayerType playerType, Vector3 origin, out Vector3 position)
        {
            CaptureSitePresenter closestSite = null;
            float closestDistance = float.MaxValue;
            foreach (CaptureSitePresenter site in _sites)
            {
                if (!site.IsCapturable || site.Owner == playerType)
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

        public bool TryGetThreatenedSite(PlayerType owner, out Vector3 position)
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

        public bool TryGetRaidTarget(PlayerType attacker, Vector3 origin, out Vector3 position)
        {
            CaptureSitePresenter closestSite = null;
            float closestDistance = float.MaxValue;
            foreach (CaptureSitePresenter site in _sites)
            {
                if (!site.IsOperational || site.Owner == attacker)
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

        public bool TryBuildOnOwnedSite(PlayerType playerType)
        {
            foreach (CaptureSitePresenter site in _sites)
            {
                // Paying for a site that hostile units are about to take would waste the credits.
                if (site.Owner == playerType && site.CanStartConstruction && !HasHostileUnits(site, playerType))
                {
                    return TryStartConstruction(site, ChooseFacilityType(playerType));
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
            if (site.Owner == PlayerType.Player && site.CanStartConstruction)
            {
                TryStartConstruction(site, facilityType);
            }
        }

        /// <summary>Keeps a side's facilities balanced: builds whichever type it owns fewer of, mining first.</summary>
        private SiteFacilityType ChooseFacilityType(PlayerType playerType)
        {
            int miningCount = 0;
            int battleAsteroidCount = 0;
            foreach (CaptureSitePresenter site in _sites)
            {
                if (site.Owner != playerType || !site.HasFacility)
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

        private void GetStrength(CaptureSitePresenter site, out float playerStrength, out float opponentStrength)
        {
            _shipService.CountShips(position => site.Contains(position), out int playerShips, out int opponentShips);
            playerStrength = playerShips;
            opponentStrength = opponentShips;

            // A squadron is positioned at the centroid of its fighters.
            foreach (IEntity squadron in _squadrons)
            {
                if (site.Contains(squadron.GetFacade<IEntityTransformFacade>().Transform.position))
                {
                    AddStrength(squadron.PlayerType, _data.SquadronCaptureWeight,
                        ref playerStrength, ref opponentStrength);
                }
            }
        }

        private static void AddStrength(PlayerType playerType, float strength,
            ref float playerStrength, ref float opponentStrength)
        {
            if (playerType == PlayerType.Player)
            {
                playerStrength += strength;
            }
            else if (playerType == PlayerType.Opponent)
            {
                opponentStrength += strength;
            }
        }

        private bool HasHostileUnits(CaptureSitePresenter site, PlayerType owner)
        {
            GetStrength(site, out float playerStrength, out float opponentStrength);
            return owner == PlayerType.Player ? opponentStrength > 0f : playerStrength > 0f;
        }

        private ISiteFacilityBuilder GetBuilder(PlayerType playerType)
        {
            return playerType switch
            {
                PlayerType.Player => _playerBuilder.Value,
                PlayerType.Opponent => _opponentBuilder.Value,
                _ => throw new ArgumentOutOfRangeException(nameof(playerType), playerType, null)
            };
        }
    }
}
