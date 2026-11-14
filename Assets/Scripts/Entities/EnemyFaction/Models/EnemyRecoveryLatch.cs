namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public sealed class EnemyRecoveryLatch
    {
        private const float WEAK_SECONDS = 5f;
        private const float RECOVERED_SECONDS = 20f;
        private float _weakSince = -1f;
        private float _recoveredSince = -1f;
        private bool _replacementBuilt;
        private bool _hadStationLoss;

        public bool IsActive { get; private set; }
        public string Trigger { get; private set; } = "none";

        /// <param name="canRebuild">False while a backup station already stands: relocation has nothing to build,
        /// so recovery neither starts nor stays on (it would only block patrols and free placement).</param>
        public bool Update(float now, float advantage, float retreatAdvantage, bool underAttack,
            bool stationLost, bool critical, bool canRebuild = true)
        {
            bool previous = IsActive;
            if (!canRebuild)
            {
                _weakSince = -1f;
                _recoveredSince = -1f;
                _hadStationLoss = stationLost;
                if (IsActive)
                {
                    IsActive = false;
                    Trigger = "backupStanding";
                }
                return previous != IsActive;
            }
            bool weak = underAttack && advantage < retreatAdvantage;
            _weakSince = weak ? (_weakSince < 0f ? now : _weakSince) : -1f;
            if (stationLost && !_hadStationLoss || !stationLost && !underAttack && !critical)
                _replacementBuilt = false;
            _hadStationLoss = stationLost;
            if (!IsActive && !_replacementBuilt)
            {
                if (stationLost && !_replacementBuilt) Enter("stationLost");
                else if (critical) Enter("critical");
                else if (weak && now - _weakSince >= WEAK_SECONDS) Enter("fleetWeak");
            }
            bool recovered = advantage >= 1f && !underAttack && !critical;
            _recoveredSince = IsActive && recovered
                ? (_recoveredSince < 0f ? now : _recoveredSince) : -1f;
            if (IsActive && _recoveredSince >= 0f && now - _recoveredSince >= RECOVERED_SECONDS)
            {
                IsActive = false;
                Trigger = "recovered";
                _replacementBuilt = stationLost;
            }
            return previous != IsActive;
        }

        public void StationBuilt()
        {
            IsActive = false;
            Trigger = "stationBuilt";
            _replacementBuilt = true;
            _weakSince = -1f;
            _recoveredSince = -1f;
        }

        private void Enter(string trigger)
        {
            IsActive = true;
            Trigger = trigger;
            _recoveredSince = -1f;
        }
    }
}
