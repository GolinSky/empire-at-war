#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace EmpireAtWar.Services.Timing
{
    internal sealed class BattleCaptureFrame
    {
        public int UnityFrame { get; set; } = -1;
        public double RealtimeSeconds { get; set; }
        public bool IsHitch { get; set; }
        public double CaptureWriteWaitMs { get; set; }
        public BattleSystemSample[] AllocationTop { get; } = new BattleSystemSample[5];
        public BattleSystemSample[] TimeTop { get; } = new BattleSystemSample[5];
        public long CoordinatorNanoseconds { get; set; }
        public long ScriptUpdateCoordinatorNanoseconds { get; set; }
        public long AttributionOverheadNanoseconds { get; set; }
        public long[] Metrics { get; }
        public long[] MarkerCounts { get; }
        public long[] Workload { get; }

        public BattleCaptureFrame(int metrics, int markers, int workload)
        {
            Metrics = metrics == 0 ? Array.Empty<long>() : new long[metrics];
            MarkerCounts = markers == 0 ? Array.Empty<long>() : new long[markers];
            Workload = workload == 0 ? Array.Empty<long>() : new long[workload];
        }

        public void CopyFrom(BattleCaptureFrame frame)
        {
            UnityFrame = frame.UnityFrame;
            RealtimeSeconds = frame.RealtimeSeconds;
            IsHitch = frame.IsHitch;
            CaptureWriteWaitMs = frame.CaptureWriteWaitMs;
            Array.Copy(frame.AllocationTop, AllocationTop, 5);
            Array.Copy(frame.TimeTop, TimeTop, 5);
            CoordinatorNanoseconds = frame.CoordinatorNanoseconds;
            ScriptUpdateCoordinatorNanoseconds = frame.ScriptUpdateCoordinatorNanoseconds;
            AttributionOverheadNanoseconds = frame.AttributionOverheadNanoseconds;
            Array.Copy(frame.Metrics, Metrics, Metrics.Length);
            Array.Copy(frame.MarkerCounts, MarkerCounts, MarkerCounts.Length);
            Array.Copy(frame.Workload, Workload, Workload.Length);
        }
    }
}
#endif
