namespace EmpireAtWar.Services.Networking
{
    /// <summary>Phase 3 defaults (Multiplayer plan); tune after the Phase 0 spike measurements.</summary>
    public static class NetworkTickSettings
    {
        public const int NETWORK_TICK_RATE = 20;
        public const float TICK_INTERVAL = 1f / NETWORK_TICK_RATE;
        /// <summary>Unreliable payload per message; stays under the transport MTU.</summary>
        public const int CHUNK_BYTE_BUDGET = 1000;
        public const float POSITION_ERROR_THRESHOLD = 2f;
        public const float HEADING_ERROR_THRESHOLD = 5f;
        public const float MOVING_REFRESH_SECONDS = 1f;
        public const float IDLE_REFRESH_SECONDS = 3f;
        /// <summary>Client render delay = TICK_INTERVAL × this = 150 ms.</summary>
        public const int INTERPOLATION_BUFFER_TICKS = 3;
        /// <summary>
        /// Render-delay drift, in ticks, past which Mirror slows the client timeline down or speeds it up. Mirror's
        /// ±1 tick let the delay rest at its low edge (≈ 1.5 ticks, underruns after 20 min; Multiplayer Connection
        /// Analysis, 2026-11-23); the low side is the one that runs out of keyframes.
        /// </summary>
        public const float SLOWDOWN_THRESHOLD_TICKS = -0.25f;
        public const float CATCHUP_THRESHOLD_TICKS = 0.5f;
    }
}
