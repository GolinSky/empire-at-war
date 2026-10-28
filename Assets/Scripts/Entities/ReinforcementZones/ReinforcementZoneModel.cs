using EmpireAtWar.Entities.Capture;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.ReinforcementZones
{
    public sealed class ReinforcementZoneModel : Model
    {
        private readonly CaptureControl _capture;
        private readonly bool _isCapturable;
        public PlayerId Owner => _capture.Owner;
        public PlayerId CapturingPlayer => _capture.CapturingPlayer;
        public float CaptureProgress => _capture.CaptureProgress;
        public bool IsContested => _capture.IsContested;

        public ReinforcementZoneModel(IPlayerRelations relations, PlayerId startingOwner,
            float captureDuration, float captureSpeedPerNetShip, bool isCapturable)
        {
            _isCapturable = isCapturable;
            _capture = new CaptureControl(relations, startingOwner,
                captureDuration > 0f ? captureDuration : 1f,
                captureSpeedPerNetShip > 0f ? captureSpeedPerNetShip : 1f);
        }

        public bool Tick(float deltaTime, CaptureStrength tally) => _capture.Tick(deltaTime, tally, _isCapturable);

        public void Restore(PlayerId owner, PlayerId capturingPlayer, float captureProgress, bool isContested) =>
            _capture.Restore(owner, capturingPlayer, captureProgress, isContested);
    }
}
