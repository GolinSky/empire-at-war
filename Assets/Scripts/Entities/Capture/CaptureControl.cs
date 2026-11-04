using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Capture
{
    public sealed class CaptureControl
    {
        private const float NEUTRALIZATION_DURATION_MULTIPLIER = 2f;

        private readonly IPlayerRelations _relations;
        private readonly float _captureDuration;
        private readonly float _captureSpeedPerNetShip;
        public PlayerId Owner { get; private set; }
        public PlayerId CapturingPlayer { get; private set; }
        public float CaptureProgress { get; private set; }
        public bool IsContested { get; private set; }
        public PlayerId ContestingFirst { get; private set; }
        public PlayerId ContestingSecond { get; private set; }

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
            ContestingFirst = isCapturable && tally.PresentTeamCount >= 2 ? tally.LeadingPlayer : PlayerId.None;
            ContestingSecond = isCapturable && tally.PresentTeamCount >= 2 ? tally.OpposingPlayer : PlayerId.None;
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
            float duration = Owner == PlayerId.None
                ? _captureDuration
                : _captureDuration * NEUTRALIZATION_DURATION_MULTIPLIER;
            CaptureProgress += deltaTime / duration * tally.Advantage * _captureSpeedPerNetShip;
            if (CaptureProgress < 1f) return false;
            SetOwner(Owner == PlayerId.None ? capturingPlayer : PlayerId.None);
            return true;
        }

        public void Restore(PlayerId owner, PlayerId capturingPlayer, float captureProgress, bool isContested)
        {
            Owner = owner;
            CapturingPlayer = capturingPlayer;
            CaptureProgress = captureProgress;
            IsContested = isContested;
            ContestingFirst = PlayerId.None;
            ContestingSecond = PlayerId.None;
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
            ContestingFirst = PlayerId.None;
            ContestingSecond = PlayerId.None;
        }

        public void ResetCapture()
        {
            CapturingPlayer = PlayerId.None;
            CaptureProgress = 0f;
        }
    }
}
