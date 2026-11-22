using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>
    /// Appends one <see cref="NetworkStatsRow"/> per call and flushes, so a crash keeps every second written.
    /// Header = "# key,value" comment lines, then the column names of the first row.
    /// </summary>
    public sealed class NetworkStatsCsvWriter : IDisposable
    {
        private readonly TextWriter _writer;
        private readonly IReadOnlyList<KeyValuePair<string, string>> _comments;
        private readonly StringBuilder _line = new StringBuilder(1024);
        private bool _headerWritten;

        public NetworkStatsCsvWriter(TextWriter writer, IReadOnlyList<KeyValuePair<string, string>> comments)
        {
            _writer = writer;
            _comments = comments;
        }

        public void Write(double battleSeconds, NetworkStatsRow row)
        {
            if (!_headerWritten) WriteHeader(row);
            _line.Clear();
            _line.Append(battleSeconds.ToString("0.0", CultureInfo.InvariantCulture));
            for (int i = 0; i < row.Count; i++)
            {
                _line.Append(',');
                double value = row.GetValue(i);
                if (!double.IsNaN(value)) _line.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
            }
            _writer.WriteLine(_line.ToString());
            _writer.Flush();
        }

        public void Dispose() => _writer.Dispose();

        private void WriteHeader(NetworkStatsRow row)
        {
            _headerWritten = true;
            foreach (KeyValuePair<string, string> comment in _comments)
                _writer.WriteLine($"# {comment.Key},{comment.Value}");
            _line.Clear();
            _line.Append("time_s");
            for (int i = 0; i < row.Count; i++) _line.Append(',').Append(row.GetName(i));
            _writer.WriteLine(_line.ToString());
        }
    }
}
