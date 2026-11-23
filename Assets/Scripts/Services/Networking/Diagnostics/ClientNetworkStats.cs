using System;
using System.Collections.Generic;
using System.Globalization;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using Mirror;
using Steamworks;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>
    /// Client columns: host link, render timeline, keyframe arrival and correction size, and the host's own
    /// numbers from <see cref="NetworkDebugStatsMessage"/>. Reliable lag = newest unreliable keyframe time minus
    /// the reliable message's send time (same host clock): how far the ordered reliable stream trails.
    /// Squadron drift = horizontal host pose vs local fighter centroid (see <see cref="SquadronDriftStats"/>).
    /// </summary>
    public sealed class ClientNetworkStats : INetworkRoleStats, IInitializable, ILateDisposable
    {
        private readonly ClientReplicaBuffer _replica;
        private readonly IEntityLocator _entities;
        private readonly SteamLinkAggregate _steam = new SteamLinkAggregate();
        private readonly SquadronDriftStats _drift = new SquadronDriftStats();
        private NetworkDebugStatsMessage _host;
        private bool _hasHost;
        private double _lagLast = double.NaN;
        private double _lagMax = double.NaN;

        public ClientNetworkStats(ClientReplicaBuffer replica, IEntityLocator entities)
        {
            _replica = replica;
            _entities = entities;
        }

        public void Initialize() => NetworkClient.RegisterHandler<NetworkDebugStatsMessage>(OnHostStats);

        public void LateDispose() => NetworkClient.UnregisterHandler<NetworkDebugStatsMessage>();

        public void Collect(NetworkStatsRow row, List<string> details, List<NetworkLinkSample> links,
            NetworkStatsWindow window)
        {
            float seconds = window.Seconds;
            double rtt = NetworkTime.rtt * 1000.0;
            row.Set("rtt_ms", rtt);
            row.Set("rtt_sd_ms", Math.Sqrt(NetworkTime.rttVariance) * 1000.0);
            _steam.Reset();
            bool hasSteam = NetworkLinkProbe.TryGetClientSteamStatus(out SteamNetConnectionRealTimeStatus_t status);
            if (hasSteam) _steam.Add(status);
            _steam.WriteTo(row);
            links.Add(new NetworkLinkSample("Host", rtt, hasSteam, status));

            ClientReplicaStats stats = _replica.Stats;
            float chunks = stats.Chunks / seconds;
            // A host that stopped ticking (match over) sends no keyframes; the delay would only count down.
            row.Set("timeline_delay_ms", _hasHost && _host.TickRate <= 0f ? double.NaN : stats.TimelineDelay * 1000.0);
            row.Set("underruns_s", stats.Underruns / seconds);
            row.Set("keyframes_s", stats.Keyframes / seconds);
            row.Set("squadron_keyframes_s", stats.InterpolatedKeyframes / seconds);
            row.Set("keyframe_chunks_s", chunks);
            row.Set("lost_chunks_s", stats.LostChunks / seconds);
            // Squadron roots are interpolated, not predicted: corrections are ships, stations and other units.
            row.Set("corrections_s", stats.Corrections / seconds);
            row.Set("correction_mean_m", stats.Corrections > 0 ? stats.PositionErrorSum / stats.Corrections : double.NaN);
            row.Set("correction_max_m", stats.Corrections > 0 ? stats.PositionErrorMax : double.NaN);
            row.Set("correction_heading_mean_deg", stats.Corrections > 0 ? stats.HeadingErrorSum / stats.Corrections : double.NaN);
            row.Set("correction_heading_max_deg", stats.Corrections > 0 ? stats.HeadingErrorMax : double.NaN);
            row.Set("entities", _replica.EntityCount);
            stats.ResetWindow();
            CollectSquadronDrift(row, details);

            row.Set("host_fps", _hasHost ? _host.HostFps : double.NaN);
            row.Set("host_tick_rate", _hasHost ? _host.TickRate : double.NaN);
            row.Set("host_shots_s", _hasHost ? _host.ShotsPerSecond : double.NaN);
            row.Set("host_keyframe_chunks_s", _hasHost ? _host.KeyframeChunksPerSecond : double.NaN);
            row.Set("host_entities", _hasHost ? _host.EntityCount : double.NaN);
            row.Set("reliable_lag_ms", _lagLast);
            row.Set("reliable_lag_max_ms", _lagMax);
            details.Add(_hasHost
                ? string.Format(CultureInfo.InvariantCulture, "Shots/s: local {0:0} vs host {1:0}; host FPS {2:0}, tick {3:0.0} Hz",
                    window.ShotsPerSecond, _host.ShotsPerSecond, _host.HostFps, _host.TickRate)
                : "No host stats received yet.");
            _lagLast = double.NaN;
            _lagMax = double.NaN;
        }

        private void CollectSquadronDrift(NetworkStatsRow row, List<string> details)
        {
            _drift.Reset();
            foreach (long entityId in _replica.EntityIds)
            {
                if (!_entities.TryGetEntity(entityId, out IEntity entity) || entity.HealthModel.IsDestroyed ||
                    !entity.TryGetFacade(out ISquadronFighterSpreadFacade squadron) ||
                    !_replica.TrySample(entityId, out EntityMotion host)) continue;
                Vector3 offset = squadron.FighterCentroid - host.Position;
                offset.y = 0f;
                _drift.Add(offset.magnitude, squadron.LoiterRadius);
            }
            bool any = _drift.Count > 0;
            row.Set("squadrons", _drift.Count);
            row.Set("squadron_drift_mean_m", _drift.Mean);
            row.Set("squadron_drift_max_m", any ? _drift.Max : double.NaN);
            row.Set("squadrons_beyond_loiter", _drift.BeyondLoiter);
            if (any)
                details.Add(string.Format(CultureInfo.InvariantCulture,
                    "Squadron drift (host pose vs local fighters): mean {0:0} m, max {1:0} m, {2}/{3} beyond loiter radius",
                    _drift.Mean, _drift.Max, _drift.BeyondLoiter, _drift.Count));
        }

        private void OnHostStats(NetworkDebugStatsMessage message)
        {
            _host = message;
            _hasHost = true;
            _lagLast = (_replica.NewestRemoteTime - message.SendTime) * 1000.0;
            if (double.IsNaN(_lagMax) || _lagLast > _lagMax) _lagMax = _lagLast;
        }
    }
}
