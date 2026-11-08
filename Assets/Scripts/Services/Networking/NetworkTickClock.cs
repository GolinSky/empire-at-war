namespace EmpireAtWar.Services.Networking
{
    /// <summary>Fixed 20 Hz network clock driven by the variable-dt simulation frame.</summary>
    public sealed class NetworkTickClock
    {
        private float _accumulator;

        public uint Tick { get; private set; }

        /// <summary>Adds frame time and returns how many network ticks are due now.</summary>
        public int Advance(float deltaTime)
        {
            _accumulator += deltaTime;
            int due = 0;
            while (_accumulator >= NetworkTickSettings.TICK_INTERVAL)
            {
                _accumulator -= NetworkTickSettings.TICK_INTERVAL;
                Tick++;
                due++;
            }

            return due;
        }
    }
}
