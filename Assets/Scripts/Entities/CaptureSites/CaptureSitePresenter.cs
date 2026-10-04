using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.CaptureSites
{
    public sealed class CaptureSitePresenter : ICaptureSite, IDisposable
    {
        private readonly ICaptureSiteView _view;
        private readonly ILocalPlayer _localPlayer;

        private static readonly SiteFacilityType[] FacilityTypes =
            (SiteFacilityType[])Enum.GetValues(typeof(SiteFacilityType));
        private readonly CaptureSiteModel _model;
        private readonly CaptureSiteData _data;
        private readonly TooltipRequests _tooltips;
        private readonly TooltipHoverSubscription _tooltipHover;
        private readonly Predicate<float> _canAfford;

        private bool _isSelected;

        public event Action<CaptureSitePresenter, SiteFacilityType> BuildRequested;

        public PlayerId Owner => _model.Owner;
        public CaptureSiteState State => _model.State;
        public float CaptureProgress => _model.CaptureProgress;
        public float ConstructionProgress => _model.ConstructionProgress;
        public bool IsCapturable => _model.IsCapturable;
        public bool CanStartConstruction => _model.CanStartConstruction;
        public bool CanPlayerBuild => _localPlayer.IsFriendly(_model.Owner) && _model.CanStartConstruction;
        public SiteFacilityType FacilityType => _model.FacilityType;
        public Vector3 Center => _view.Center;
        public Vector3 FacilityPosition => _view.FacilityPosition;
        public float Radius => _view.Radius;
        public bool IsRevealed { get; private set; }
        public bool IsOperational => _model.State == CaptureSiteState.Operational;
        public bool HasFacility => _model.State is CaptureSiteState.Constructing or CaptureSiteState.Operational;

        public CaptureSitePresenter(
            ICaptureSiteView view,
            ILocalPlayer localPlayer,
            ITooltipService tooltipService,
            CaptureSiteModel model,
            CaptureSiteData data,
            Predicate<float> canAfford)
        {
            _localPlayer = localPlayer;
            _model = model;
            _view = view;
            _data = data;
            _canAfford = canAfford;
            _tooltips = new TooltipRequests(tooltipService);
            _tooltipHover = new TooltipHoverSubscription(
                ((ITooltipHoverView)view).TooltipHover, HandleTooltipHover, _tooltips);
            foreach (SiteFacilityType facilityType in FacilityTypes)
            {
                SiteFacilityCost cost = data.GetCost(facilityType);
                _view.ConfigureOption(facilityType, cost.Name.ToUpperInvariant(),
                    $"{cost.Price:0} CR   {cost.BuildTime:0}s");
            }

            _view.BuildPressed += HandleBuildPressed;
            Render();
            SetVisibility(false, false, _ => false);
        }

        public void Dispose()
        {
            _view.BuildPressed -= HandleBuildPressed;
            _tooltipHover.Dispose();
        }

        public SiteFacilityCost GetCost(SiteFacilityType facilityType)
        {
            return _data.GetCost(facilityType);
        }

        public bool Contains(Vector3 position, float clearance = 0f)
        {
            float x = position.x - Center.x;
            float z = position.z - Center.z;
            float radius = Radius + clearance;
            return x * x + z * z <= radius * radius;
        }

        /// <returns>True when the site changed owner.</returns>
        public bool TickCapture(float deltaTime, CaptureStrength tally)
        {
            return _model.TickCapture(deltaTime, tally);
        }

        /// <returns>True on the frame the facility finishes construction.</returns>
        public bool TickConstruction(float deltaTime)
        {
            return _model.TickConstruction(deltaTime);
        }

        public void StartConstruction(SiteFacilityType facilityType)
        {
            _isSelected = false;
            _model.StartConstruction(facilityType, _data.GetCost(facilityType).BuildTime);
        }

        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
        }

        public void ReleaseFacility()
        {
            _model.ReleaseFacility();
        }

        public void Render()
        {
            _view.Render(_localPlayer.GetRelation(_model.Owner), _model.State, _model.FacilityType,
                _localPlayer.GetRelation(_model.CapturingPlayer),
                _model.CaptureProgress, _model.ConstructionProgress, _model.IsContested);
        }

        public void SetVisibility(bool isVisible, bool isHovered, Predicate<float> canPlayerAfford)
        {
            IsRevealed = isVisible;
            // The facility choice only appears after the player selects a friendly empty site.
            bool showBuildOptions = isVisible && _isSelected && CanPlayerBuild;
            bool isActive = _model.CapturingPlayer != PlayerId.None || _model.IsContested ||
                _model.State == CaptureSiteState.Constructing;
            // The ring is known terrain; ownership details need current vision.
            _view.SetVisibility(true, isVisible && (isHovered || isActive || showBuildOptions));
            _view.SetBuildOptionsVisible(showBuildOptions);
            if (!showBuildOptions)
            {
                return;
            }

            foreach (SiteFacilityType facilityType in FacilityTypes)
            {
                _view.SetOptionInteractable(facilityType, canPlayerAfford(_data.GetCost(facilityType).Price));
            }
        }

        private void HandleBuildPressed(SiteFacilityType facilityType)
        {
            BuildRequested?.Invoke(this, facilityType);
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source)
        {
            _tooltips.Show(source, key, anchor, () => IsRevealed, () =>
            {
                if (key is SiteFacilityType type)
                {
                    SiteFacilityCost cost = _data.GetCost(type);
                    return new TooltipContent(title: cost.Name,
                        description: $"Construct {cost.Name} at this friendly capture site.", stats: new[]
                        {
                            new TooltipStat(label: "Cost (credits)", current: cost.Price),
                            new TooltipStat(label: "Build time (s)", current: cost.BuildTime)
                        }, requirements: new[]
                        {
                            new TooltipRequirement("Friendly empty site", CanPlayerBuild),
                            new TooltipRequirement($"{cost.Price:0} credits", _canAfford(cost.Price))
                        });
                }
                return new TooltipContent(title: "Capture site",
                    description: "Move units into the ring to capture it. Select a friendly empty site to choose a facility.",
                    stats: new[] { new TooltipStat(label: "Capture (%)", current: CaptureProgress * 100f),
                        new TooltipStat(label: "Construction (%)", current: ConstructionProgress * 100f) },
                    status: $"Owner: {Owner} · {State} · {FacilityType}");
            });
        }
    }
}
