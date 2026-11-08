using System.Collections.Generic;
using EmpireAtWar.Services.Networking.Messages;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Client side of tick replication: binds net indices, buffers keyframes per entity and keeps a render
    /// timeline <see cref="NetworkTickSettings.INTERPOLATION_BUFFER_TICKS"/> ticks behind the host
    /// (Mirror <see cref="SnapshotInterpolation"/>). Applying the sampled motion to client entities is Phase 4.
    /// </summary>
    public sealed class ClientReplicaBuffer
    {
        private readonly SortedList<double, TickTimelineSnapshot> _timeline = new SortedList<double, TickTimelineSnapshot>();
        private readonly SnapshotInterpolationSettings _settings = new SnapshotInterpolationSettings
        {
            bufferTimeMultiplier = NetworkTickSettings.INTERPOLATION_BUFFER_TICKS
        };
        private readonly NetIndexMap _indices = new NetIndexMap();
        private readonly Dictionary<long, ClientKeyframeTrack> _tracks = new Dictionary<long, ClientKeyframeTrack>();
        private ExponentialMovingAverage _driftEma;
        private ExponentialMovingAverage _deliveryTimeEma;
        private double _localTimeline;
        private double _localTimescale = 1;
        private uint _lastTimelineTick;

        public ClientReplicaBuffer()
        {
            _driftEma = new ExponentialMovingAverage(NetworkTickSettings.NETWORK_TICK_RATE * _settings.driftEmaDuration);
            _deliveryTimeEma = new ExponentialMovingAverage(NetworkTickSettings.NETWORK_TICK_RATE * _settings.deliveryTimeEmaDuration);
        }

        public double RenderTime => _localTimeline;
        public double BufferTime => NetworkTickSettings.TICK_INTERVAL * _settings.bufferTimeMultiplier;
        public uint LastReceivedTick { get; private set; }
        public int EntityCount => _tracks.Count;

        public void OnSpawn(EntitySpawnMessage message, double localTime)
        {
            _indices.Bind(message.Index, message.EntityId);
            ClientKeyframeTrack track = new ClientKeyframeTrack();
            track.Add(message.Time, EntityKeyframeCodec.Decode(message.Keyframe));
            _tracks.Add(message.EntityId, track);
            AdvanceTimeline(message.Tick, message.Time, localTime);
        }

        public void OnDespawn(EntityDespawnMessage message)
        {
            if (!_indices.TryGetEntityId(message.Index, out long entityId)) return;
            _tracks.Remove(entityId);
            _indices.Unbind(message.Index);
        }

        public void OnKeyframes(EntityKeyframesMessage message, double localTime)
        {
            AdvanceTimeline(message.Tick, message.Time, localTime);
            foreach (EntityKeyframe keyframe in message.Keyframes)
            {
                // Unreliable keyframes can arrive before their reliable spawn; the next refresh repairs it.
                if (_indices.TryGetEntityId(keyframe.Index, out long entityId))
                    _tracks[entityId].Add(message.Time, EntityKeyframeCodec.Decode(keyframe));
            }
        }

        public void Update(double unscaledDeltaTime)
        {
            if (_timeline.Count == 0) return;
            SnapshotInterpolation.Step(_timeline, unscaledDeltaTime, ref _localTimeline, _localTimescale, out _, out _, out _);
        }

        public bool TrySample(long entityId, out EntityMotion motion)
        {
            motion = default;
            if (!_tracks.TryGetValue(entityId, out ClientKeyframeTrack track) || !track.HasSamples) return false;
            motion = track.Sample(_localTimeline);
            return true;
        }

        private void AdvanceTimeline(uint tick, double remoteTime, double localTime)
        {
            if (tick > LastReceivedTick) LastReceivedTick = tick;
            if (tick == _lastTimelineTick) return;
            _lastTimelineTick = tick;
            SnapshotInterpolation.InsertAndAdjust(_timeline, _settings.bufferLimit,
                new TickTimelineSnapshot(remoteTime, localTime), ref _localTimeline, ref _localTimescale,
                NetworkTickSettings.TICK_INTERVAL, BufferTime, _settings.catchupSpeed, _settings.slowdownSpeed,
                ref _driftEma, _settings.catchupNegativeThreshold, _settings.catchupPositiveThreshold,
                ref _deliveryTimeEma);
        }
    }
}
