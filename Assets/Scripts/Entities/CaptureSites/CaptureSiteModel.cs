using System;
using EmpireAtWar.Entities.Capture;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.CaptureSites
{
    /// <summary>
    /// Neutral → Owned → Constructing → Operational; the owner picks the facility when construction starts
    /// and losing the facility returns the site to Neutral.
    /// An operational facility locks the site: it must be destroyed before the site can be captured.
    /// </summary>
    public sealed class CaptureSiteModel : Model
    {
        private readonly CaptureControl _capture;
        private float _buildDuration;

        public event Action<PlayerId, PlayerId, SiteFacilityType> Captured;
        public event Action<PlayerId, SiteFacilityType> Lost;

        public PlayerId Owner => _capture.Owner;
        public CaptureSiteState State { get; private set; } = CaptureSiteState.Neutral;
        public PlayerId CapturingPlayer => _capture.CapturingPlayer;
        public float CaptureProgress => _capture.CaptureProgress;
        public SiteFacilityType FacilityType { get; private set; }
        public float ConstructionProgress { get; private set; }
        public bool IsContested => IsCapturable && _capture.IsContested;
        public bool IsCapturable => State != CaptureSiteState.Operational;
        public bool CanStartConstruction => State == CaptureSiteState.Owned;

        public CaptureSiteModel(IPlayerRelations relations, float captureDuration, float captureSpeedPerNetShip,
            PlayerId startingOwner = default, SiteFacilityType facilityType = SiteFacilityType.Mining)
        {
            FacilityType = facilityType;
            _capture = new CaptureControl(relations, startingOwner, captureDuration, captureSpeedPerNetShip);
            State = startingOwner == PlayerId.None ? CaptureSiteState.Neutral : CaptureSiteState.Owned;
        }

        public bool TickCapture(float deltaTime, CaptureStrength tally)
        {
            PlayerId previousOwner = Owner;
            if (!IsCapturable) _capture.ResetCapture();
            if (!_capture.Tick(deltaTime, tally, IsCapturable)) return false;
            State = CaptureSiteState.Owned;
            ConstructionProgress = 0f;
            Captured?.Invoke(previousOwner, Owner, FacilityType);
            return true;
        }

        public void StartConstruction(SiteFacilityType facilityType, float buildDuration)
        {
            FacilityType = facilityType;
            _buildDuration = buildDuration;
            ConstructionProgress = 0f;
            State = CaptureSiteState.Constructing;
        }

        /// <returns>True on the frame the construction completes.</returns>
        public bool TickConstruction(float deltaTime)
        {
            if (State != CaptureSiteState.Constructing)
            {
                return false;
            }

            ConstructionProgress += deltaTime / _buildDuration;
            if (ConstructionProgress < 1f)
            {
                return false;
            }

            ConstructionProgress = 1f;
            State = CaptureSiteState.Operational;
            return true;
        }

        /// <summary>Loads a saved site; contested flags are recomputed on the next capture tick.</summary>
        public void Restore(PlayerId owner, CaptureSiteState state, PlayerId capturingPlayer, float captureProgress,
            SiteFacilityType facilityType, float constructionProgress, float buildDuration)
        {
            _capture.Restore(owner, capturingPlayer, captureProgress, false);
            State = state;
            FacilityType = facilityType;
            ConstructionProgress = constructionProgress;
            _buildDuration = buildDuration;
        }

        public void ReleaseFacility()
        {
            PlayerId previousOwner = Owner;
            _capture.Release();
            State = CaptureSiteState.Neutral;
            ConstructionProgress = 0f;
            if (previousOwner != PlayerId.None) Lost?.Invoke(previousOwner, FacilityType);
        }

    }
}
