using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
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
    /// Owns the hand-placed capture sites of a map: ship-driven capture, paid construction
    /// through each side's <see cref="ISiteFacilityBuilder"/>, and reset when the facility dies.
    /// </summary>
    public sealed class CaptureSitesSystem : MonoBehaviour, ICaptureSitesSystem, IInitializable, ITickable,
        ILateDisposable
    {
        // Explored fog retains 0.35 visibility; site status requires current vision.
        private const float MINIMUM_SITE_VISIBILITY = 0.5f;

        [SerializeField] private CaptureSiteView[] siteViews = Array.Empty<CaptureSiteView>();

        private readonly List<CaptureSitePresenter> _sites = new List<CaptureSitePresenter>();
        private IShipService _shipService;
        private CaptureSiteData _data;
        private FogOfWarSystem _fogOfWarSystem;
        private ICameraService _cameraService;
        private IInputService _inputService;
        private LazyInject<ISiteFacilityBuilder> _playerBuilder;
        private LazyInject<ISiteFacilityBuilder> _opponentBuilder;
        private CaptureSitePresenter _selectedSite;

        [Inject]
        private void Construct(
            IShipService shipService,
            CaptureSiteData data,
            FogOfWarSystem fogOfWarSystem,
            ICameraService cameraService,
            IInputService inputService,
            [Inject(Id = PlayerType.Player)] LazyInject<ISiteFacilityBuilder> playerBuilder,
            [Inject(Id = PlayerType.Opponent)] LazyInject<ISiteFacilityBuilder> opponentBuilder)
        {
            _shipService = shipService;
            _data = data;
            _fogOfWarSystem = fogOfWarSystem;
            _cameraService = cameraService;
            _inputService = inputService;
            _playerBuilder = playerBuilder;
            _opponentBuilder = opponentBuilder;
        }

        public void Initialize()
        {
            foreach (CaptureSiteView view in siteViews)
            {
                CaptureSiteModel model = new CaptureSiteModel(view.CaptureDuration, _data.CaptureSpeedPerNetShip);
                CaptureSitePresenter site = new CaptureSitePresenter(model, view, _data.GetCost(view.FacilityType));
                site.BuildRequested += HandleBuildRequested;
                _sites.Add(site);
            }

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

            foreach (CaptureSitePresenter site in _sites)
            {
                CountShips(site, out int playerShips, out int opponentShips);
                site.TickCapture(deltaTime, playerShips, opponentShips);
                if (site.TickConstruction(deltaTime))
                {
                    GetBuilder(site.Owner).Build(site.FacilityType, site.Center, site.ReleaseFacility);
                }

                site.Render();
                bool isVisible = !_fogOfWarSystem.IsHidden(site.Center, MINIMUM_SITE_VISIBILITY);
                bool isHovered = isVisible && _inputService.SupportsHover &&
                    site.Contains(_cameraService.GetWorldPoint(_inputService.TouchPosition, site.Center));
                bool canPlayerAfford = site.Owner == PlayerType.Player &&
                    _playerBuilder.Value.CanAfford(site.Cost.Price);
                site.SetVisibility(isVisible, isHovered, canPlayerAfford);
            }
        }

        public bool IsPositionInAnySite(Vector3 position, float clearance = 0f)
        {
            // Reads the views directly so zone layout can query sites before Initialize.
            foreach (CaptureSiteView view in siteViews)
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

        public bool TryBuildOnOwnedSite(PlayerType playerType)
        {
            foreach (CaptureSitePresenter site in _sites)
            {
                if (site.Owner == playerType && site.CanStartConstruction)
                {
                    return TryStartConstruction(site);
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

        private void HandleBuildRequested(CaptureSitePresenter site)
        {
            if (site.Owner == PlayerType.Player && site.CanStartConstruction)
            {
                TryStartConstruction(site);
            }
        }

        private bool TryStartConstruction(CaptureSitePresenter site)
        {
            if (!GetBuilder(site.Owner).TrySpend(site.Cost.Price))
            {
                return false;
            }

            if (site == _selectedSite)
            {
                _selectedSite = null;
            }

            site.StartConstruction();
            return true;
        }

        private void CountShips(CaptureSitePresenter site, out int playerShips, out int opponentShips)
        {
            playerShips = 0;
            opponentShips = 0;
            foreach (IShipEntity ship in _shipService.Ships)
            {
                if (!site.Contains(ship.WorldPosition))
                {
                    continue;
                }

                if (ship.PlayerType == PlayerType.Player)
                {
                    playerShips++;
                }
                else if (ship.PlayerType == PlayerType.Opponent)
                {
                    opponentShips++;
                }
            }
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
