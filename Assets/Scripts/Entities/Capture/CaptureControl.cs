using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Capture
{
    public sealed class CaptureControl
    {
        private readonly IPlayerRelations _relations;
        private readonly float _captureDuration;
        private readonly float _captureSpeedPerNetShip;
        public PlayerId Owner { get; private set; }
        public PlayerId CapturingPlayer { get; private set; }
        public float CaptureProgress { get; private set; }
        public bool IsContested { get; private set; }

        public CaptureControl(IPlayerRelations relations, PlayerId startingOwner, float captureDuration,
            float captureSpeedPerNetShip)
        {
            _relations = relations;
            Owner = startingOwner;
            _captureDuration = captureDuration;
            _captureSpeedPerNetShip = captureSpeedPerNetShip;
        }

        public bool Tick(float deltaTime, CaptureStrength tally, bool isCapturable)
        {
            IsContested = tally.IsContested;
            if (!isCapturable) return false;
            if (!tally.HasUnits)
            {
                ResetCapture();
                return false;
            }
            if (tally.IsTied) return false;
            PlayerId capturingPlayer = tally.LeadingPlayer;
            if (_relations.IsAllied(capturingPlayer, Owner))
            {
                ResetCapture();
                return false;
            }
            if (!_relations.IsAllied(CapturingPlayer, capturingPlayer)) CaptureProgress = 0f;
            CapturingPlayer = capturingPlayer;
            CaptureProgress += deltaTime / _captureDuration * tally.Advantage * _captureSpeedPerNetShip;
            if (CaptureProgress < 1f) return false;
            SetOwner(capturingPlayer);
            return true;
        }

        public void Restore(PlayerId owner, PlayerId capturingPlayer, float captureProgress, bool isContested)
        {
            Owner = owner;
            CapturingPlayer = capturingPlayer;
            CaptureProgress = captureProgress;
            IsContested = isContested;
        }

        public void SetOwner(PlayerId owner)
        {
            Owner = owner;
            ResetCapture();
        }

        public void Release()
        {
            SetOwner(PlayerId.None);
            IsContested = false;
        }

        public void ResetCapture()
        {
            CapturingPlayer = PlayerId.None;
            CaptureProgress = 0f;
        }
    }
}
