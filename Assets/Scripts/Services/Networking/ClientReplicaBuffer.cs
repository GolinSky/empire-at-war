using System.Collections.Generic;
using EmpireAtWar.Services.Networking.Diagnostics;
using EmpireAtWar.Services.Networking.Messages;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Client side of tick replication: binds net indices, buffers keyframes per entity and keeps a render
    /// timeline <see cref="NetworkTickSettings.INTERPOLATION_BUFFER_TICKS"/> ticks behind the host
    /// (Mirror <see cref="SnapshotInterpolation"/>). <see cref="ClientReplicationReceiver"/> applies the sampled motion to client entities.
    /// </summary>
    public sealed class ClientReplicaBuffer
    {
        private readonly SortedList<double, TickTimelineSnapshot> _timeline = new SortedList<double, TickTimelineSnapshot>();
        private readonly SnapshotInterpolationSettings _settings = new SnapshotInterpolationSettings
        {
            bufferTimeMultiplier = NetworkTickSettings.INTERPOLATION_BUFFER_TICKS,
            catchupNegativeThreshold = NetworkTickSettings.SLOWDOWN_THRESHOLD_TICKS,
            catchupPositiveThreshold = NetworkTickSettings.CATCHUP_THRESHOLD_TICKS
        };
        private readonly NetIndexMap _indices = new NetIndexMap();
        private readonly Dictionary<long, ClientKeyframeTrack> _tracks = new Dictionary<long, ClientKeyframeTrack>();
        // Sent every tick without rates (EntitySpawnMessage.IsInterpolated): a prediction error is just the tick's motion.
        private readonly HashSet<long> _interpolated = new HashSet<long>();
        private ExponentialMovingAverage _driftEma;
        private ExponentialMovingAverage _deliveryTimeEma;
        private double _localTimeline;
        private double _localTimescale = 1;
        private uint _lastTimelineTick;
        private double _newestRemoteTime;

        public ClientReplicaBuffer()
        {
            _driftEma = new ExponentialMovingAverage(NetworkTickSettings.NETWORK_TICK_RATE * _settings.driftEmaDuration);
            _deliveryTimeEma = new ExponentialMovingAverage(NetworkTickSettings.NETWORK_TICK_RATE * _settings.deliveryTimeEmaDuration);
        }

        public double RenderTime => _localTimeline;
        public double BufferTime => NetworkTickSettings.TICK_INTERVAL * _settings.bufferTimeMultiplier;
        public uint LastReceivedTick { get; private set; }
        /// <summary>Host time of the newest tick received; render time trails it by about <see cref="BufferTime"/>.</summary>
        public double NewestRemoteTime => _newestRemoteTime;
        public ClientReplicaStats Stats { get; } = new ClientReplicaStats();
        public int EntityCount => _tracks.Count;

        public IEnumerable<long> EntityIds => _tracks.Keys;

        public bool TryGetEntityId(ushort index, out long entityId) => _indices.TryGetEntityId(index, out entityId);

        /// <summary>False when the entity is already tracked (a rejoin baseline can repeat a same-frame spawn).</summary>
        public bool OnSpawn(EntitySpawnMessage message, double localTime)
        {
            if (_tracks.ContainsKey(message.EntityId)) return false;
            _indices.Bind(message.Index, message.EntityId);
            ClientKeyframeTrack track = new ClientKeyframeTrack();
            track.Add(message.Time, EntityKeyframeCodec.Decode(message.Keyframe));
            _tracks.Add(message.EntityId, track);
            if (message.IsInterpolated) _interpolated.Add(message.EntityId);
            AdvanceTimeline(message.Tick, message.Time, localTime);
            return true;
        }

        public void OnDespawn(EntityDespawnMessage message)
        {
            if (!_indices.TryGetEntityId(message.Index, out long entityId)) return;
            _tracks.Remove(entityId);
            _interpolated.Remove(entityId);
            _indices.Unbind(message.Index);
        }

        public void OnKeyframes(EntityKeyframesMessage message, double localTime)
        {
            AdvanceTimeline(message.Tick, message.Time, localTime);
            Stats.RecordChunk(message.Tick, message.ChunkCount, message.Keyframes.Length);
            foreach (EntityKeyframe keyframe in message.Keyframes)
            {
                // Unreliable keyframes can arrive before their reliable spawn; the next refresh repairs it.
                if (!_indices.TryGetEntityId(keyframe.Index, out long entityId)) continue;
                ClientKeyframeTrack track = _tracks[entityId];
                EntityMotion motion = EntityKeyframeCodec.Decode(keyframe);
                if (_interpolated.Contains(entityId)) Stats.RecordInterpolatedKeyframe();
                else if (track.TryPredict(message.Time, out EntityMotion predicted)) Stats.RecordCorrection(motion, predicted);
                track.Add(message.Time, motion);
            }
        }

        public void Update(double unscaledDeltaTime)
        {
            if (_timeline.Count == 0) return;
            SnapshotInterpolation.Step(_timeline, unscaledDeltaTime, ref _localTimeline, _localTimescale, out _, out _, out _);
            Stats.RecordFrame(_newestRemoteTime - _localTimeline);
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
            if (remoteTime > _newestRemoteTime) _newestRemoteTime = remoteTime;
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
