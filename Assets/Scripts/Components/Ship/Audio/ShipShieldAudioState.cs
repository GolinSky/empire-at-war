namespace EmpireAtWar.Components.Ship.Audio
{
    public sealed class ShipShieldAudioState
    {
        private bool _down;
        private bool _hasShields;
        private float _downReadyAt;
        private float _upReadyAt;

        public void Reset(float shields)
        {
            _hasShields |= shields > 0f;
            _down = shields <= 0f;
            _downReadyAt = 0f;
            _upReadyAt = 0f;
        }

        // -1: down, 1: restored above the hysteresis threshold, 0: no audible transition.
        public int Advance(float shields, float percentage, float now, float upThreshold, float cooldown)
        {
            if (!_hasShields)
            {
                if (shields > 0f) Reset(shields);
                return 0;
            }
            if (!_down && shields <= 0f)
            {
                _down = true;
                if (now < _downReadyAt) return 0;
                _downReadyAt = now + cooldown;
                return -1;
            }
            if (_down && percentage >= upThreshold)
            {
                _down = false;
                if (now < _upReadyAt) return 0;
                _upReadyAt = now + cooldown;
                return 1;
            }
            return 0;
        }
    }
}
