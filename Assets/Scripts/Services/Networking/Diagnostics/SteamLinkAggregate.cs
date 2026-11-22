using System;
using System.Globalization;
using Steamworks;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>
    /// Steam link columns: the worst value over every link (host: one per client; client: the host link).
    /// Quality = fraction of packets delivered (Steam's estimate, −1 = unknown).
    /// </summary>
    public sealed class SteamLinkAggregate
    {
        private int _links;
        private int _pingMax;
        private float _qualityLocalMin;
        private float _qualityRemoteMin;
        private double _outBytes;
        private double _inBytes;
        private int _sendRateMin;
        private int _pendingReliableMax;
        private int _pendingUnreliableMax;
        private int _unackedReliableMax;
        private double _queueMaxMs;

        public void Reset()
        {
            _links = 0;
            _pingMax = 0;
            _qualityLocalMin = float.MaxValue;
            _qualityRemoteMin = float.MaxValue;
            _outBytes = 0;
            _inBytes = 0;
            _sendRateMin = int.MaxValue;
            _pendingReliableMax = 0;
            _pendingUnreliableMax = 0;
            _unackedReliableMax = 0;
            _queueMaxMs = 0;
        }

        public void Add(in SteamNetConnectionRealTimeStatus_t status)
        {
            _links++;
            _pingMax = Math.Max(_pingMax, status.m_nPing);
            _qualityLocalMin = Math.Min(_qualityLocalMin, status.m_flConnectionQualityLocal);
            _qualityRemoteMin = Math.Min(_qualityRemoteMin, status.m_flConnectionQualityRemote);
            _outBytes += status.m_flOutBytesPerSec;
            _inBytes += status.m_flInBytesPerSec;
            _sendRateMin = Math.Min(_sendRateMin, status.m_nSendRateBytesPerSecond);
            _pendingReliableMax = Math.Max(_pendingReliableMax, status.m_cbPendingReliable);
            _pendingUnreliableMax = Math.Max(_pendingUnreliableMax, status.m_cbPendingUnreliable);
            _unackedReliableMax = Math.Max(_unackedReliableMax, status.m_cbSentUnackedReliable);
            _queueMaxMs = Math.Max(_queueMaxMs, status.m_usecQueueTime.m_SteamNetworkingMicroseconds / 1000.0);
        }

        public void WriteTo(NetworkStatsRow row)
        {
            bool any = _links > 0;
            row.Set("steam_links", _links);
            row.Set("steam_ping_ms", any ? _pingMax : double.NaN);
            row.Set("steam_quality_local", any ? _qualityLocalMin : double.NaN);
            row.Set("steam_quality_remote", any ? _qualityRemoteMin : double.NaN);
            row.Set("steam_out_Bps", any ? _outBytes : double.NaN);
            row.Set("steam_in_Bps", any ? _inBytes : double.NaN);
            row.Set("steam_send_rate_Bps", any ? _sendRateMin : double.NaN);
            row.Set("steam_pending_reliable_B", any ? _pendingReliableMax : double.NaN);
            row.Set("steam_pending_unreliable_B", any ? _pendingUnreliableMax : double.NaN);
            row.Set("steam_unacked_reliable_B", any ? _unackedReliableMax : double.NaN);
            row.Set("steam_queue_ms", any ? _queueMaxMs : double.NaN);
        }

        public static string FormatLink(string label, double rttMs, bool hasSteam, in SteamNetConnectionRealTimeStatus_t status)
        {
            string rtt = $"{label}: rtt {rttMs.ToString("0", CultureInfo.InvariantCulture)} ms";
            if (!hasSteam) return rtt + ", no Steam link status (direct IP)";
            return rtt + string.Format(CultureInfo.InvariantCulture,
                ", Steam ping {0} ms, quality {1:0.###}/{2:0.###}, out {3:0.0} KiB/s, in {4:0.0} KiB/s, rate {5:0} KiB/s, pending rel {6} B / unrel {7} B, unacked {8} B, queue {9:0.0} ms",
                status.m_nPing, status.m_flConnectionQualityLocal, status.m_flConnectionQualityRemote,
                status.m_flOutBytesPerSec / 1024f, status.m_flInBytesPerSec / 1024f,
                status.m_nSendRateBytesPerSecond / 1024f, status.m_cbPendingReliable, status.m_cbPendingUnreliable,
                status.m_cbSentUnackedReliable, status.m_usecQueueTime.m_SteamNetworkingMicroseconds / 1000.0);
        }
    }
}
