using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.ReinforcementZones
{
    public sealed class ReinforcementZoneModel : Model
    {
        private readonly IPlayerRelations _relations;

        private readonly float _captureDuration;
        private readonly float _captureSpeedPerNetShip;

        private readonly bool _isCapturable;

        public PlayerId Owner { get; private set; }
        public PlayerId CapturingPlayer { get; private set; } = PlayerId.None;
        public float CaptureProgress { get; private set; }
        public bool IsContested { get; private set; }

        public ReinforcementZoneModel(
            IPlayerRelations relations,
            PlayerId startingOwner,
            float captureDuration,
            float captureSpeedPerNetShip,
            bool isCapturable)
        {
            Owner = startingOwner;
            _isCapturable = isCapturable;
            _captureDuration = captureDuration > 0f ? captureDuration : 1f;
            _captureSpeedPerNetShip = captureSpeedPerNetShip > 0f ? captureSpeedPerNetShip : 1f;
            _relations = relations;
        }

        /// <returns>True when the zone changed owner.</returns>
        public bool Tick(float deltaTime, CaptureStrength tally)
        {
            IsContested = tally.IsContested;

            if (!_isCapturable)
            {
                return false;
            }

            if (tally.IsTied)
            {
                if (!tally.HasUnits)
                {
                    ResetCapture();
                }

                return false;
            }

            // Allies never take a zone from each other.
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

            Owner = capturingPlayer;
            ResetCapture();
            return true;
        }

        private void ResetCapture()
        {
            CapturingPlayer = PlayerId.None;
            CaptureProgress = 0f;
        }
    }
}
