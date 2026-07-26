using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Models.ReinforcementZones
{
    public sealed class ReinforcementZoneModel
    {
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

        public bool Tick(float deltaTime, int playerShipCount, int opponentShipCount)
        {
            int shipAdvantage = playerShipCount - opponentShipCount;
            IsContested = playerShipCount > 0 && opponentShipCount > 0 && shipAdvantage == 0;

            if (!_isCapturable)
            {
                return false;
            }

            if (shipAdvantage == 0)
            {
                if (playerShipCount == 0 && opponentShipCount == 0)
                {
                    ResetCapture();
                }

                return false;
            }

            PlayerType capturingPlayer = shipAdvantage > 0
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

            int netShipCount = System.Math.Abs(shipAdvantage);
            CaptureProgress += deltaTime / _captureDuration * netShipCount * _captureSpeedPerNetShip;
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
