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
        public long[] Metrics { get; }
        public long[] MarkerCounts { get; }
        public long[] Workload { get; }

        public BattleCaptureFrame(int metrics, int markers, int workload)
        {
            Metrics = new long[metrics];
            MarkerCounts = new long[markers];
            Workload = new long[workload];
        }

        public void CopyFrom(BattleCaptureFrame frame)
        {
            UnityFrame = frame.UnityFrame;
            RealtimeSeconds = frame.RealtimeSeconds;
            IsHitch = frame.IsHitch;
            CaptureWriteWaitMs = frame.CaptureWriteWaitMs;
            Array.Copy(frame.Metrics, Metrics, Metrics.Length);
            Array.Copy(frame.MarkerCounts, MarkerCounts, MarkerCounts.Length);
            Array.Copy(frame.Workload, Workload, Workload.Length);
        }
    }
}
#endif
