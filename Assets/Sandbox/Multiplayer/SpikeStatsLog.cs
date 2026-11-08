using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    /// <summary>
    /// One CSV per session in <c>spike_logs</c> beside the executable (project root in the Editor):
    /// a row per second with the panel's measurements, so runs on two machines can be compared afterwards.
    /// </summary>
    public sealed class SpikeStatsLog : IDisposable
    {
        private const string FOLDER = "spike_logs";
        private const string HEADER = "time_s,role,transport,mode,units_total,clients,tick," +
            "per_client_kib_s,per_client_peak_kib_s,host_upload_kib_s,down_kib_s,down_peak_kib_s," +
            "rtt_ms,render_delay_ms,timeline_buffer,keyframes_per_tick," +
            "dr_capital_mean_m,dr_capital_max_m,dr_squadron_mean_m,dr_squadron_max_m," +
            "command_seq,command_ack_ms,command_visible_ms,fps";

        private readonly StreamWriter _writer;
        private readonly double _startTime;

        public SpikeStatsLog(string role, string transport, double startTime)
        {
            string folder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), FOLDER);
            Directory.CreateDirectory(folder);
            FilePath = System.IO.Path.Combine(folder,
                $"spike_{role}_{transport}_{DateTime.Now:yyyyMMdd_HHmmss}.csv".ToLowerInvariant());
            _writer = new StreamWriter(FilePath) { AutoFlush = true };
            _writer.WriteLine(HEADER);
            _startTime = startTime;
            Debug.Log($"[MultiplayerSpike] Stats log: {FilePath}");
        }

        public string FilePath { get; }

        public void Write(double now, params object[] values)
        {
            _writer.Write((now - _startTime).ToString("0.0", CultureInfo.InvariantCulture));
            foreach (object value in values)
            {
                _writer.Write(',');
                _writer.Write(value is IFormattable formattable
                    ? formattable.ToString(value is float || value is double ? "0.###" : null, CultureInfo.InvariantCulture)
                    : value);
            }

            _writer.WriteLine();
        }

        public void Dispose() => _writer.Dispose();
    }
}
