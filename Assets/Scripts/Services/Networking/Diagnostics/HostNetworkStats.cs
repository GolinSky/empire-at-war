using System.Collections.Generic;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using Mirror;
using Steamworks;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>
    /// Host columns: real tick rate, keyframe output, replicated entities, client links (worst value over
    /// clients); also sends <see cref="NetworkDebugStatsMessage"/> to every loaded client once per window.
    /// </summary>
    public sealed class HostNetworkStats : INetworkRoleStats
    {
        private readonly NetworkTickService _ticks;
        private readonly OnlineMatchService _online;
        private readonly SteamLinkAggregate _steam = new SteamLinkAggregate();
        private uint _lastTick;
        private long _lastKeyframes;
        private long _lastChunks;

        public HostNetworkStats(NetworkTickService ticks, OnlineMatchService online)
        {
            _ticks = ticks;
            _online = online;
        }

        public void Collect(NetworkStatsRow row, List<string> details, NetworkStatsWindow window)
        {
            float seconds = window.Seconds;
            float tickRate = (_ticks.CurrentTick - _lastTick) / seconds;
            float chunks = (_ticks.KeyframeChunksSent - _lastChunks) / seconds;
            row.Set("tick_rate", tickRate);
            row.Set("keyframes_s", (_ticks.KeyframesSent - _lastKeyframes) / seconds);
            row.Set("keyframe_chunks_s", chunks);
            row.Set("entities", _ticks.ReplicatedEntityCount);
            _lastTick = _ticks.CurrentTick;
            _lastKeyframes = _ticks.KeyframesSent;
            _lastChunks = _ticks.KeyframeChunksSent;

            int clients = 0;
            double rttMax = double.NaN;
            _steam.Reset();
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (!_online.IsBattleLoaded(connection)) continue;
                clients++;
                double rtt = connection.rtt * 1000.0;
                if (double.IsNaN(rttMax) || rtt > rttMax) rttMax = rtt;
                bool hasSteam = NetworkLinkProbe.TryGetServerSteamStatus(connection.connectionId,
                    out SteamNetConnectionRealTimeStatus_t status);
                if (hasSteam) _steam.Add(status);
                details.Add(SteamLinkAggregate.FormatLink($"Client {connection.connectionId}", rtt, hasSteam, status));
            }
            row.Set("clients", clients);
            row.Set("rtt_ms", rttMax);
            _steam.WriteTo(row);

            if (!_online.HasBegun) return;
            HostBroadcast.ToClients(_online, new NetworkDebugStatsMessage
            {
                SendTime = NetworkTime.time,
                HostFps = window.Fps,
                TickRate = tickRate,
                ShotsPerSecond = window.ShotsPerSecond,
                KeyframeChunksPerSecond = chunks,
                EntityCount = _ticks.ReplicatedEntityCount
            });
        }
    }
}
