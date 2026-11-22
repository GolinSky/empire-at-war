using System;
using System.Collections.Generic;
using System.Globalization;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using Mirror;
using Steamworks;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>
    /// Client columns: host link, render timeline, keyframe arrival and correction size, and the host's own
    /// numbers from <see cref="NetworkDebugStatsMessage"/>. Reliable lag = newest unreliable keyframe time minus
    /// the reliable message's send time (same host clock): how far the ordered reliable stream trails.
    /// </summary>
    public sealed class ClientNetworkStats : INetworkRoleStats, IInitializable, ILateDisposable
    {
        private readonly ClientReplicaBuffer _replica;
        private readonly SteamLinkAggregate _steam = new SteamLinkAggregate();
        private NetworkDebugStatsMessage _host;
        private bool _hasHost;
        private double _lagLast = double.NaN;
        private double _lagMax = double.NaN;

        public ClientNetworkStats(ClientReplicaBuffer replica) => _replica = replica;

        public void Initialize() => NetworkClient.RegisterHandler<NetworkDebugStatsMessage>(OnHostStats);

        public void LateDispose() => NetworkClient.UnregisterHandler<NetworkDebugStatsMessage>();

        public void Collect(NetworkStatsRow row, List<string> details, NetworkStatsWindow window)
        {
            float seconds = window.Seconds;
            double rtt = NetworkTime.rtt * 1000.0;
            row.Set("rtt_ms", rtt);
            row.Set("rtt_sd_ms", Math.Sqrt(NetworkTime.rttVariance) * 1000.0);
            _steam.Reset();
            bool hasSteam = NetworkLinkProbe.TryGetClientSteamStatus(out SteamNetConnectionRealTimeStatus_t status);
            if (hasSteam) _steam.Add(status);
            _steam.WriteTo(row);
            details.Add(SteamLinkAggregate.FormatLink("Host", rtt, hasSteam, status));

            ClientReplicaStats stats = _replica.Stats;
            float chunks = stats.Chunks / seconds;
            row.Set("timeline_delay_ms", stats.TimelineDelay * 1000.0);
            row.Set("underruns_s", stats.Underruns / seconds);
            row.Set("keyframes_s", stats.Keyframes / seconds);
            row.Set("keyframe_chunks_s", chunks);
            row.Set("lost_chunks_s", stats.LostChunks / seconds);
            row.Set("corrections_s", stats.Corrections / seconds);
            row.Set("correction_mean_m", stats.Corrections > 0 ? stats.PositionErrorSum / stats.Corrections : double.NaN);
            row.Set("correction_max_m", stats.Corrections > 0 ? stats.PositionErrorMax : double.NaN);
            row.Set("correction_heading_mean_deg", stats.Corrections > 0 ? stats.HeadingErrorSum / stats.Corrections : double.NaN);
            row.Set("correction_heading_max_deg", stats.Corrections > 0 ? stats.HeadingErrorMax : double.NaN);
            row.Set("entities", _replica.EntityCount);
            stats.ResetWindow();

            row.Set("host_fps", _hasHost ? _host.HostFps : double.NaN);
            row.Set("host_tick_rate", _hasHost ? _host.TickRate : double.NaN);
            row.Set("host_shots_s", _hasHost ? _host.ShotsPerSecond : double.NaN);
            row.Set("host_keyframe_chunks_s", _hasHost ? _host.KeyframeChunksPerSecond : double.NaN);
            row.Set("host_entities", _hasHost ? _host.EntityCount : double.NaN);
            // Approximate: the host's last window against this one; ticks lost whole show up only here.
            row.Set("chunk_loss_pct", _hasHost && _host.KeyframeChunksPerSecond > 0f
                ? Math.Max(0.0, 1.0 - chunks / _host.KeyframeChunksPerSecond) * 100.0 : double.NaN);
            row.Set("reliable_lag_ms", _lagLast);
            row.Set("reliable_lag_max_ms", _lagMax);
            details.Add(_hasHost
                ? string.Format(CultureInfo.InvariantCulture, "Shots/s: local {0:0} vs host {1:0}; host FPS {2:0}, tick {3:0.0} Hz",
                    window.ShotsPerSecond, _host.ShotsPerSecond, _host.HostFps, _host.TickRate)
                : "No host stats received yet.");
            _lagLast = double.NaN;
            _lagMax = double.NaN;
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
