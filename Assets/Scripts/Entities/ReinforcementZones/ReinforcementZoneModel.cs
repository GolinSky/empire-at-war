using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Models.ReinforcementZones
{
    public sealed class ReinforcementZoneModel
    {
        private readonly bool _isCapturable;
        private readonly float _captureDuration;

        public ReinforcementZoneModel(PlayerType startingOwner, bool isCapturable, float captureDuration)
        {
            Owner = startingOwner;
            _isCapturable = isCapturable;
            _captureDuration = captureDuration > 0f ? captureDuration : 1f;
        }

        public PlayerType Owner { get; private set; }
        public PlayerType CapturingPlayer { get; private set; } = PlayerType.None;
        public float CaptureProgress { get; private set; }
        public bool IsContested { get; private set; }

        public bool Tick(float deltaTime, int playerShipCount, int opponentShipCount)
        {
            IsContested = playerShipCount > 0 && opponentShipCount > 0;

            if (!_isCapturable || IsContested)
            {
                return false;
            }

            PlayerType capturingPlayer = GetCapturingPlayer(playerShipCount, opponentShipCount);
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

            CaptureProgress += deltaTime / _captureDuration;
            if (CaptureProgress < 1f)
            {
                return false;
            }

            Owner = capturingPlayer;
            ResetCapture();
            return true;
        }

        private static PlayerType GetCapturingPlayer(int playerShipCount, int opponentShipCount)
        {
            if (playerShipCount > 0 && opponentShipCount == 0)
            {
                return PlayerType.Player;
            }

            if (opponentShipCount > 0 && playerShipCount == 0)
            {
                return PlayerType.Opponent;
            }

            return PlayerType.None;
        }

        private void ResetCapture()
        {
            CapturingPlayer = PlayerType.None;
            CaptureProgress = 0f;
        }
    }
}
