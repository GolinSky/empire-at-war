using EmpireAtWar.Utils;
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
        private readonly CaptureSiteModel _model;
        private readonly CaptureSiteData _data;
        private readonly TooltipRequests _tooltips;
        private readonly TooltipHoverSubscription _tooltipHover;
        private SiteFacilityType? _renderedFacilityType;
        private string _facilityName;

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
        public PlayerId CapturingPlayer => _model.CapturingPlayer;
        /// <summary>Entity id of the operational facility; 0 while the site has none.</summary>
        public long FacilityId { get; private set; }

        public CaptureSitePresenter(
            ICaptureSiteView view,
            ILocalPlayer localPlayer,
            ITooltipService tooltipService,
            CaptureSiteModel model,
            CaptureSiteData data)
        {
            _localPlayer = localPlayer;
            _model = model;
            _view = view;
            _data = data;
            _tooltips = new TooltipRequests(tooltipService);
            _tooltipHover = new TooltipHoverSubscription(
                view.TooltipHover, HandleTooltipHover, _tooltips);
            Render();
            SetVisibility(false, false);
        }

        public void Dispose()
        {
            _tooltipHover.Dispose();
        }

        public SiteFacilityCost GetCost(SiteFacilityType facilityType)
        {
            return _data.GetCost(facilityType);
        }

        public bool Contains(Vector3 position, float clearance = 0f)
        {
            return PlanarGeometry.ContainsCircle(position, Center, Radius + clearance);
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
            _model.StartConstruction(facilityType, _data.GetCost(facilityType).BuildTime);
        }

        public void SetFacility(long facilityId) => FacilityId = facilityId;

        public void ReleaseFacility()
        {
            FacilityId = 0;
            _model.ReleaseFacility();
        }

        public void Restore(PlayerId owner, CaptureSiteState state, PlayerId capturingPlayer, float captureProgress,
            SiteFacilityType facilityType, float constructionProgress)
        {
            _model.Restore(owner, state, capturingPlayer, captureProgress, facilityType, constructionProgress,
                _data.GetCost(facilityType).BuildTime);
            Render();
        }

        public void Render()
        {
            if (_renderedFacilityType != _model.FacilityType)
            {
                _renderedFacilityType = _model.FacilityType;
                _facilityName = _data.GetCost(_model.FacilityType).Name.ToUpperInvariant();
            }
            _view.Render(_localPlayer.GetRelation(_model.Owner), _model.State, _model.FacilityType, _facilityName,
                _localPlayer.GetRelation(_model.CapturingPlayer),
                _model.CaptureProgress, _model.ConstructionProgress, _model.IsContested);
        }

        public void SetVisibility(bool isVisible, bool isHovered)
        {
            IsRevealed = isVisible;
            bool isActive = _model.CapturingPlayer != PlayerId.None || _model.IsContested ||
                _model.State == CaptureSiteState.Constructing;
            // The ring is known terrain; ownership details need current vision.
            _view.SetVisibility(true, isVisible && (isHovered || isActive));
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source)
        {
            _tooltips.Show(source, key, anchor, () => IsRevealed, () =>
            {
                return new TooltipContent(title: "Capture site",
                    description: "Move units into the ring to capture it. Select a friendly empty site to choose a facility.",
                    stats: new[] { new TooltipStat(label: "Capture (%)", current: CaptureProgress * 100f),
                        new TooltipStat(label: "Construction (%)", current: ConstructionProgress * 100f) },
                    status: $"Owner: {Owner} · {State} · {FacilityType}");
            });
        }
    }
}
