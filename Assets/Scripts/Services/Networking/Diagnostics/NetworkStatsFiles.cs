using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>
    /// The three CSV files of one online battle, sharing one name stem:
    /// &lt;stem&gt;_net.csv (1 row/s), &lt;stem&gt;_pools.csv (1 row/s per pool with a salvo waiting) and
    /// &lt;stem&gt;_links.csv (1 row/s per connection). A stem already taken gets _2, _3… so two game instances on
    /// one PC never share a file. A file error is logged once and stops the CSV; the overlay keeps running.
    /// </summary>
    public sealed class NetworkStatsFiles : IDisposable
    {
        private const string POOLS_HEADER =
            "time_s,pool,group,waiting,in_use,total,created_at_runtime,oldest_wait_ms,max_wait_ms";
        private const string LINKS_HEADER =
            "time_s,link,rtt_ms,steam_ping_ms,steam_quality_local,steam_quality_remote,steam_out_Bps,steam_in_Bps," +
            "steam_send_rate_Bps,steam_pending_reliable_B,steam_pending_unreliable_B,steam_unacked_reliable_B,steam_queue_ms";

        private NetworkStatsCsvWriter _stats;
        private TextWriter _pools;
        private TextWriter _links;

        private NetworkStatsFiles(string statsPath) => StatsPath = statsPath;

        /// <summary>Path of the _net.csv file; null when the files could not be created.</summary>
        public string StatsPath { get; }
        public bool IsWriting => _stats != null;

        public static NetworkStatsFiles Open(string folder, string stem, IReadOnlyList<KeyValuePair<string, string>> comments)
        {
            string statsPath = null;
            var opened = new List<StreamWriter>(3);
            try
            {
                Directory.CreateDirectory(folder);
                string prefix = null;
                for (int attempt = 1; prefix == null; attempt++)
                {
                    string candidate = Path.Combine(folder, attempt == 1 ? stem : $"{stem}_{attempt}");
                    statsPath = candidate + "_net.csv";
                    if (File.Exists(statsPath)) continue;
                    opened.Add(new StreamWriter(new FileStream(statsPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read)));
                    prefix = candidate;
                }
                opened.Add(new StreamWriter(prefix + "_pools.csv"));
                opened.Add(new StreamWriter(prefix + "_links.csv"));
                var files = new NetworkStatsFiles(statsPath)
                {
                    _stats = new NetworkStatsCsvWriter(opened[0], comments),
                    _pools = opened[1],
                    _links = opened[2],
                };
                files._pools.WriteLine(POOLS_HEADER);
                files._links.WriteLine(LINKS_HEADER);
                return files;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                foreach (StreamWriter writer in opened) writer.Dispose();
                Debug.LogError($"[NetworkStats] Battle network CSV disabled, cannot create files in '{folder}': {exception.Message}");
                return new NetworkStatsFiles(null);
            }
        }

        public void WriteStats(double battleSeconds, NetworkStatsRow row)
        {
            if (!IsWriting) return;
            try { _stats.Write(battleSeconds, row); }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Stop(exception);
            }
        }

        public void WritePool(double battleSeconds, in ShotPoolSample pool)
        {
            if (!IsWriting) return;
            Write(_pools, string.Format(CultureInfo.InvariantCulture, "{0:0.0},{1},{2},{3},{4},{5},{6},{7:0},{8:0}",
                battleSeconds, Sanitize(pool.Prefab), pool.IsFighters ? "fighters" : "ships", pool.Waiting, pool.InUse,
                pool.Total, pool.RuntimeCreated, pool.OldestWaitSeconds * 1000f, pool.WindowMaxWaitSeconds * 1000f));
        }

        public void WriteLink(double battleSeconds, in NetworkLinkSample link)
        {
            if (!IsWriting) return;
            string line = string.Format(CultureInfo.InvariantCulture, "{0:0.0},{1},{2:0.#}", battleSeconds,
                Sanitize(link.Label), link.RttMs);
            if (link.HasSteam)
            {
                var status = link.Status;
                line += string.Format(CultureInfo.InvariantCulture, ",{0},{1:0.###},{2:0.###},{3:0},{4:0},{5},{6},{7},{8},{9:0.#}",
                    status.m_nPing, status.m_flConnectionQualityLocal, status.m_flConnectionQualityRemote,
                    status.m_flOutBytesPerSec, status.m_flInBytesPerSec, status.m_nSendRateBytesPerSecond,
                    status.m_cbPendingReliable, status.m_cbPendingUnreliable, status.m_cbSentUnackedReliable,
                    status.m_usecQueueTime.m_SteamNetworkingMicroseconds / 1000.0);
            }
            else line += ",,,,,,,,,,";
            Write(_links, line);
        }

        /// <summary>Pushes the pools and links rows of this window to disk, so a crash keeps every second written.</summary>
        public void Flush()
        {
            if (!IsWriting) return;
            try
            {
                _pools.Flush();
                _links.Flush();
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Stop(exception);
            }
        }

        public void Dispose()
        {
            if (!IsWriting) return;
            _stats.Dispose();
            _pools.Dispose();
            _links.Dispose();
            _stats = null;
        }

        private void Write(TextWriter writer, string line)
        {
            try { writer.WriteLine(line); }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Stop(exception);
            }
        }

        private void Stop(Exception exception)
        {
            Debug.LogError($"[NetworkStats] Battle network CSV stopped, write to '{StatsPath}' failed: {exception.Message}");
            try { Dispose(); }
            catch (IOException) { }
            _stats = null;
        }

        private static string Sanitize(string value) => value.Replace(',', ' ');
    }
}
