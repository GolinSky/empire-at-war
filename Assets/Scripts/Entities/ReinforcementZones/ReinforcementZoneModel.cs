using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.ReinforcementZones
{
    public sealed class ReinforcementZoneModel : PureModel
    {
        private const float TIE_EPSILON = 0.001f;

        private readonly bool _isCapturable;
        private readonly float _captureDuration;
        private readonly float _captureSpeedPerNetShip;

        public ReinforcementZoneModel(
            PlayerType startingOwner,
            bool isCapturable,
            float captureDuration,
            float captureSpeedPerNetShip)
        {
            Owner = startingOwner;
            _isCapturable = isCapturable;
            _captureDuration = captureDuration > 0f ? captureDuration : 1f;
            _captureSpeedPerNetShip = captureSpeedPerNetShip > 0f ? captureSpeedPerNetShip : 1f;
        }

        public PlayerType Owner { get; private set; }
        public PlayerType CapturingPlayer { get; private set; } = PlayerType.None;
        public float CaptureProgress { get; private set; }
        public bool IsContested { get; private set; }

        /// <param name="playerStrength">Weighted count of player units in the zone (ship = 1).</param>
        /// <param name="opponentStrength">Weighted count of opponent units in the zone (ship = 1).</param>
        public bool Tick(float deltaTime, float playerStrength, float opponentStrength)
        {
            float advantage = playerStrength - opponentStrength;
            bool isTied = System.Math.Abs(advantage) < TIE_EPSILON;
            IsContested = playerStrength > 0f && opponentStrength > 0f && isTied;

            if (!_isCapturable)
            {
                return false;
            }

            if (isTied)
            {
                if (playerStrength <= 0f && opponentStrength <= 0f)
                {
                    ResetCapture();
                }

                return false;
            }

            PlayerType capturingPlayer = advantage > 0f
                ? PlayerType.Player
                : PlayerType.Opponent;
            if (capturingPlayer == PlayerType.None || capturingPlayer == Owner)
            {
                ResetCapture();
                return false;
            }

            if (CapturingPlayer != capturingPlayer)
            {
                CapturingPlayer = capturingPlayer;
                CaptureProgress = 0f;
            }

            float netStrength = System.Math.Abs(advantage);
            CaptureProgress += deltaTime / _captureDuration * netStrength * _captureSpeedPerNetShip;
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
            CapturingPlayer = PlayerType.None;
            CaptureProgress = 0f;
        }
    }
}
