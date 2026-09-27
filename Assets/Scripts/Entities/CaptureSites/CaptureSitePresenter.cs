using System;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Entities.CaptureSites
{
    public sealed class CaptureSitePresenter : IDisposable
    {
        private readonly CaptureSiteModel _model;
        private readonly ICaptureSiteView _view;
        private readonly SiteFacilityCost _cost;
        private readonly string _buildLabel;
        private bool _isSelected;

        public event Action<CaptureSitePresenter> BuildRequested;

        public CaptureSitePresenter(CaptureSiteModel model, ICaptureSiteView view, SiteFacilityCost cost)
        {
            _model = model;
            _view = view;
            _cost = cost;
            _buildLabel = $"BUILD {cost.Name.ToUpperInvariant()} - {cost.Price:0}";
            _view.BuildPressed += HandleBuildPressed;
            Render();
            SetVisibility(false, false, false);
        }

        public PlayerType Owner => _model.Owner;
        public bool IsCapturable => _model.IsCapturable;
        public bool CanStartConstruction => _model.CanStartConstruction;
        public bool CanPlayerBuild => _model.Owner == PlayerType.Player && _model.CanStartConstruction;
        public SiteFacilityType FacilityType => _view.FacilityType;
        public SiteFacilityCost Cost => _cost;
        public Vector3 Center => _view.Center;
        public float Radius => _view.Radius;
        public bool IsRevealed { get; private set; }
        public bool IsOperational => _model.State == CaptureSiteState.Operational;

        public void Dispose()
        {
            _view.BuildPressed -= HandleBuildPressed;
        }

        public bool Contains(Vector3 position, float clearance = 0f)
        {
            float x = position.x - Center.x;
            float z = position.z - Center.z;
            float radius = Radius + clearance;
            return x * x + z * z <= radius * radius;
        }

        /// <returns>True when the site changed owner.</returns>
        public bool TickCapture(float deltaTime, int playerShipCount, int opponentShipCount)
        {
            return _model.TickCapture(deltaTime, playerShipCount, opponentShipCount);
        }

        /// <returns>True on the frame the facility finishes construction.</returns>
        public bool TickConstruction(float deltaTime)
        {
            return _model.TickConstruction(deltaTime);
        }

        public void StartConstruction()
        {
            _isSelected = false;
            _model.StartConstruction(_cost.BuildTime);
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
            _view.Render(_model.Owner, _model.State, _model.CapturingPlayer, _model.CaptureProgress,
                _model.ConstructionProgress, _model.IsContested);
        }

        public void SetVisibility(bool isVisible, bool isHovered, bool canPlayerAffordBuild)
        {
            IsRevealed = isVisible;
            // The build option only appears after the player selects their empty site.
            bool showBuildOption = isVisible && _isSelected && CanPlayerBuild;
            bool isActive = _model.CapturingPlayer != PlayerType.None || _model.IsContested ||
                _model.State == CaptureSiteState.Constructing;
            _view.SetVisibility(isVisible, isHovered || isActive || showBuildOption);
            _view.SetBuildOption(showBuildOption, canPlayerAffordBuild, _buildLabel);
        }

        private void HandleBuildPressed()
        {
            BuildRequested?.Invoke(this);
        }
    }
}
