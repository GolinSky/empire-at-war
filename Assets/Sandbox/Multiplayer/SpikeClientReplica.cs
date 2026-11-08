using System.Collections.Generic;
using Mirror;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Client view of the host sim: buffers tick chunks, renders at a delayed timeline (Mirror SnapshotInterpolation).
    public sealed class SpikeClientReplica
    {
        private readonly SortedList<double, SpikeTimelineSnapshot> _timeline = new SortedList<double, SpikeTimelineSnapshot>();
        private readonly SnapshotInterpolationSettings _settings;
        private readonly SpikeUnitTrack[] _tracks;
        private readonly SpikeKinematics[] _rendered;
        private ExponentialMovingAverage _driftEma;
        private ExponentialMovingAverage _deliveryTimeEma;
        private double _localTimeline;
        private double _localTimescale = 1;
        private uint _lastTimelineTick;

        public SpikeClientReplica(SpikeWelcomeMessage welcome, SnapshotInterpolationSettings settings)
        {
            _settings = settings;
            Side = welcome.Side;
            Mode = welcome.Mode;
            UnitSides = welcome.UnitSides;
            UnitKinds = welcome.UnitKinds;
            _tracks = new SpikeUnitTrack[UnitSides.Length];
            _rendered = new SpikeKinematics[UnitSides.Length];
            for (int i = 0; i < _tracks.Length; i++)
                _tracks[i] = new SpikeUnitTrack();

            int sendRate = (int)(1f / SpikeHostSimulation.TICK_INTERVAL);
            _driftEma = new ExponentialMovingAverage(sendRate * settings.driftEmaDuration);
            _deliveryTimeEma = new ExponentialMovingAverage(sendRate * settings.deliveryTimeEmaDuration);
        }

        public byte Side { get; }
        public SpikeSyncMode Mode { get; }
        public byte[] UnitSides { get; }
        public SpikeUnitKind[] UnitKinds { get; }
        public IReadOnlyList<SpikeKinematics> Rendered => _rendered;
        public uint LastReceivedTick { get; private set; }
        public int TimelineBufferCount => _timeline.Count;
        public double BufferTime => SpikeHostSimulation.TICK_INTERVAL * _settings.bufferTimeMultiplier;
        public double RenderTime => _localTimeline;
        public int ReceivedChunks { get; private set; }

        public bool IsVisible(int index) => _tracks[index].HasSamples;

        public void OnTick(SpikeTickMessage message)
        {
            ReceivedChunks++;
            if (message.Tick > LastReceivedTick)
                LastReceivedTick = message.Tick;

            if (message.Tick != _lastTimelineTick)
            {
                _lastTimelineTick = message.Tick;
                SnapshotInterpolation.InsertAndAdjust(
                    _timeline,
                    _settings.bufferLimit,
                    new SpikeTimelineSnapshot(message.Time, NetworkTime.localTime),
                    ref _localTimeline,
                    ref _localTimescale,
                    SpikeHostSimulation.TICK_INTERVAL,
                    BufferTime,
                    _settings.catchupSpeed,
                    _settings.slowdownSpeed,
                    ref _driftEma,
                    _settings.catchupNegativeThreshold,
                    _settings.catchupPositiveThreshold,
                    ref _deliveryTimeEma);
            }

            foreach (SpikeUnitState state in message.Units)
                _tracks[state.Index].Add(message.Time, SpikeQuantization.Decode(state));
        }

        public void Update(double unscaledDeltaTime)
        {
            if (_timeline.Count == 0)
                return;

            SnapshotInterpolation.Step(_timeline, unscaledDeltaTime, ref _localTimeline, _localTimescale, out _, out _, out _);
            bool interpolate = Mode == SpikeSyncMode.FullSnapshot;
            for (int i = 0; i < _tracks.Length; i++)
            {
                if (_tracks[i].HasSamples)
                    _rendered[i] = _tracks[i].Sample(_localTimeline, interpolate);
            }
        }
    }
}
