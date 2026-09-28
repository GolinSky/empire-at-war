using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.CaptureSites
{
    /// <summary>
    /// Neutral → Owned → Constructing → Operational; the owner picks the facility when construction starts
    /// and losing the facility returns the site to Neutral.
    /// An operational facility locks the site: it must be destroyed before the site can be captured.
    /// </summary>
    public sealed class CaptureSiteModel : PureModel
    {
        private readonly float _captureDuration;
        private readonly float _captureSpeedPerNetShip;
        private readonly IPlayerRelations _relations;
        private float _buildDuration;

        public CaptureSiteModel(float captureDuration, float captureSpeedPerNetShip, IPlayerRelations relations)
        {
            _captureDuration = captureDuration;
            _captureSpeedPerNetShip = captureSpeedPerNetShip;
            _relations = relations;
        }

        public PlayerId Owner { get; private set; } = PlayerId.None;
        public CaptureSiteState State { get; private set; } = CaptureSiteState.Neutral;
        public PlayerId CapturingPlayer { get; private set; } = PlayerId.None;
        public float CaptureProgress { get; private set; }
        public SiteFacilityType FacilityType { get; private set; }
        public float ConstructionProgress { get; private set; }
        public bool IsContested { get; private set; }
        public bool IsCapturable => State != CaptureSiteState.Operational;
        public bool CanStartConstruction => State == CaptureSiteState.Owned;

        /// <returns>True when the site changed owner.</returns>
        public bool TickCapture(float deltaTime, CaptureTally tally)
        {
            IsContested = IsCapturable && tally.IsContested;

            if (!IsCapturable || !tally.HasUnits)
            {
                ResetCapture();
                return false;
            }

            if (tally.IsTied)
            {
                return false;
            }

            // Allies never take a site from each other.
            PlayerId capturingPlayer = tally.LeadingPlayer;
            if (_relations.IsAllied(capturingPlayer, Owner))
            {
                ResetCapture();
                return false;
            }

            // Progress carries over while the same team keeps capturing, even if its lead player changes.
            if (!_relations.IsAllied(CapturingPlayer, capturingPlayer))
            {
                CaptureProgress = 0f;
            }

            CapturingPlayer = capturingPlayer;
            CaptureProgress += deltaTime / _captureDuration * tally.Advantage * _captureSpeedPerNetShip;
            if (CaptureProgress < 1f)
            {
                return false;
            }

            // Capturing grants the location only; an unfinished build is lost.
            Owner = capturingPlayer;
            State = CaptureSiteState.Owned;
            ConstructionProgress = 0f;
            ResetCapture();
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

        public void ReleaseFacility()
        {
            Owner = PlayerId.None;
            State = CaptureSiteState.Neutral;
            ConstructionProgress = 0f;
            ResetCapture();
        }

        private void ResetCapture()
        {
            CapturingPlayer = PlayerId.None;
            CaptureProgress = 0f;
        }
    }
}
