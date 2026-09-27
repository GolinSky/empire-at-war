using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.CaptureSites
{
    /// <summary>
    /// Neutral → Owned → Constructing → Operational; losing the facility returns the site to Neutral.
    /// An operational facility locks the site: it must be destroyed before the site can be captured.
    /// </summary>
    public sealed class CaptureSiteModel : PureModel
    {
        private readonly float _captureDuration;
        private readonly float _captureSpeedPerNetShip;
        private float _buildDuration;

        public CaptureSiteModel(float captureDuration, float captureSpeedPerNetShip)
        {
            _captureDuration = captureDuration;
            _captureSpeedPerNetShip = captureSpeedPerNetShip;
        }

        public PlayerType Owner { get; private set; } = PlayerType.None;
        public CaptureSiteState State { get; private set; } = CaptureSiteState.Neutral;
        public PlayerType CapturingPlayer { get; private set; } = PlayerType.None;
        public float CaptureProgress { get; private set; }
        public float ConstructionProgress { get; private set; }
        public bool IsContested { get; private set; }
        public bool IsCapturable => State != CaptureSiteState.Operational;
        public bool CanStartConstruction => State == CaptureSiteState.Owned;

        /// <returns>True when the site changed owner.</returns>
        public bool TickCapture(float deltaTime, int playerShipCount, int opponentShipCount)
        {
            int shipAdvantage = playerShipCount - opponentShipCount;
            IsContested = IsCapturable && playerShipCount > 0 && opponentShipCount > 0 && shipAdvantage == 0;

            if (!IsCapturable || (playerShipCount == 0 && opponentShipCount == 0))
            {
                ResetCapture();
                return false;
            }

            if (shipAdvantage == 0)
            {
                return false;
            }

            PlayerType capturingPlayer = shipAdvantage > 0 ? PlayerType.Player : PlayerType.Opponent;
            if (capturingPlayer == Owner)
            {
                ResetCapture();
                return false;
            }

            if (CapturingPlayer != capturingPlayer)
            {
                CapturingPlayer = capturingPlayer;
                CaptureProgress = 0f;
            }

            int netShipCount = System.Math.Abs(shipAdvantage);
            CaptureProgress += deltaTime / _captureDuration * netShipCount * _captureSpeedPerNetShip;
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

        public void StartConstruction(float buildDuration)
        {
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
            Owner = PlayerType.None;
            State = CaptureSiteState.Neutral;
            ConstructionProgress = 0f;
            ResetCapture();
        }

        private void ResetCapture()
        {
            CapturingPlayer = PlayerType.None;
            CaptureProgress = 0f;
        }
    }
}
