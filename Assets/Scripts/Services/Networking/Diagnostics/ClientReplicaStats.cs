using UnityEngine;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>
    /// Client replication counters for the network status overlay; <see cref="ResetWindow"/> starts the next
    /// one-second window. Correction = distance between the newest keyframe and what the client predicted for
    /// that time: the pop the blend has to hide.
    /// </summary>
    public sealed class ClientReplicaStats
    {
        private uint _chunkTick;
        private int _chunkExpected;
        private int _chunkReceived;
        private bool _inUnderrun;

        public int Keyframes { get; private set; }
        /// <summary>Keyframes of interpolated entities (squadron roots), sent every tick and kept out of the correction stats.</summary>
        public int InterpolatedKeyframes { get; private set; }
        public int Chunks { get; private set; }
        /// <summary>Chunks missing from ticks that arrived partly. Ticks lost whole are not visible here.</summary>
        public int LostChunks { get; private set; }
        public int Corrections { get; private set; }
        public double PositionErrorSum { get; private set; }
        public float PositionErrorMax { get; private set; }
        public double HeadingErrorSum { get; private set; }
        public float HeadingErrorMax { get; private set; }
        /// <summary>Times the render timeline passed the newest tick (motion extrapolated with nothing ahead).</summary>
        public int Underruns { get; private set; }
        /// <summary>Newest received host time minus render time, seconds; negative while underrun.</summary>
        public double TimelineDelay { get; private set; }

        public void RecordChunk(uint tick, int chunkCount, int keyframes)
        {
            Chunks++;
            Keyframes += keyframes;
            if (tick < _chunkTick) return;
            if (tick > _chunkTick)
            {
                if (_chunkTick != 0) LostChunks += Mathf.Max(0, _chunkExpected - _chunkReceived);
                _chunkTick = tick;
                _chunkExpected = chunkCount;
                _chunkReceived = 0;
            }
            _chunkReceived++;
        }

        public void RecordInterpolatedKeyframe() => InterpolatedKeyframes++;

        public void RecordCorrection(EntityMotion received, EntityMotion predicted)
        {
            float position = Vector3.Distance(received.Position, predicted.Position);
            float heading = Mathf.Abs(Mathf.DeltaAngle(received.Heading, predicted.Heading));
            Corrections++;
            PositionErrorSum += position;
            HeadingErrorSum += heading;
            if (position > PositionErrorMax) PositionErrorMax = position;
            if (heading > HeadingErrorMax) HeadingErrorMax = heading;
        }

        public void RecordFrame(double timelineDelay)
        {
            TimelineDelay = timelineDelay;
            bool underrun = timelineDelay < 0;
            if (underrun && !_inUnderrun) Underruns++;
            _inUnderrun = underrun;
        }

        public void ResetWindow()
        {
            Keyframes = 0;
            InterpolatedKeyframes = 0;
            Chunks = 0;
            LostChunks = 0;
            Corrections = 0;
            PositionErrorSum = 0;
            PositionErrorMax = 0f;
            HeadingErrorSum = 0;
            HeadingErrorMax = 0f;
            Underruns = 0;
        }
    }
}
