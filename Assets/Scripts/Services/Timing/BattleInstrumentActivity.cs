#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace EmpireAtWar.Services.Timing
{
    /// <summary>
    /// Marks frames disturbed by the measurement tools themselves (Profiler saves, allocation decoding,
    /// memory snapshots) so the audit scripts can exclude them from gameplay numbers.
    /// </summary>
    public static class BattleInstrumentActivity
    {
        private const int TAIL_FRAMES = 5;
        private static int _busyThroughFrame = -1;

        /// <summary>Call on the main thread immediately before instrument work that can stall the Editor.</summary>
        public static void MarkBusy() => _busyThroughFrame = Time.frameCount + TAIL_FRAMES;

        internal static bool IsBusy(int completedFrame) => completedFrame <= _busyThroughFrame;
    }
}
#endif
