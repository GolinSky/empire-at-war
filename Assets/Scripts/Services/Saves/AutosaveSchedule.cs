namespace EmpireAtWar.Services.Saves
{
    public sealed class AutosaveSchedule
    {
        public const float INTERVAL_SECONDS = 300f;
        private float _nextSave = -1f;
        private bool _waveCompleted;

        public void RequestWaveSave() => _waveCompleted = true;

        public bool TakeDueSave(float elapsed, bool canSave)
        {
            if (!canSave) return false;
            if (_nextSave < 0f) _nextSave = elapsed + INTERVAL_SECONDS;
            if (!_waveCompleted && elapsed < _nextSave) return false;
            _waveCompleted = false;
            _nextSave = elapsed + INTERVAL_SECONDS;
            return true;
        }
    }
}
