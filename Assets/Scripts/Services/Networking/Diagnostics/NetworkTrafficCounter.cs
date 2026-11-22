using System;
using System.Collections.Generic;
using Mirror;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>
    /// Counts Mirror message traffic per type through <see cref="NetworkDiagnostics"/> (fires in release builds).
    /// Our side keeps counters only; Mirror itself boxes each message to raise the event.
    /// </summary>
    public sealed class NetworkTrafficCounter : IDisposable
    {
        private readonly Dictionary<Type, MessageTraffic> _types = new Dictionary<Type, MessageTraffic>();

        public NetworkTrafficCounter()
        {
            NetworkDiagnostics.OutMessageEvent += OnOut;
            NetworkDiagnostics.InMessageEvent += OnIn;
        }

        public int InMessages { get; private set; }
        public long InBytes { get; private set; }
        public int OutMessages { get; private set; }
        public long OutBytes { get; private set; }
        public Dictionary<Type, MessageTraffic>.ValueCollection Types => _types.Values;

        public MessageTraffic Get(Type type)
        {
            if (!_types.TryGetValue(type, out MessageTraffic traffic))
                _types.Add(type, traffic = new MessageTraffic(type.Name));
            return traffic;
        }

        public void ResetWindow()
        {
            InMessages = 0;
            InBytes = 0;
            OutMessages = 0;
            OutBytes = 0;
            foreach (MessageTraffic traffic in _types.Values) traffic.ResetWindow();
        }

        public void Dispose()
        {
            NetworkDiagnostics.OutMessageEvent -= OnOut;
            NetworkDiagnostics.InMessageEvent -= OnIn;
        }

        // count = connections the message went to; bytes are per copy.
        private void OnOut(NetworkDiagnostics.MessageInfo info) => RecordOut(info.message.GetType(), info.count, info.bytes);

        private void OnIn(NetworkDiagnostics.MessageInfo info) => RecordIn(info.message.GetType(), info.count, info.bytes);

        /// <summary>One sent message: <paramref name="bytesPerCopy"/> went to <paramref name="copies"/> connections.</summary>
        public void RecordOut(Type type, int copies, int bytesPerCopy)
        {
            long bytes = (long)bytesPerCopy * copies;
            OutMessages += copies;
            OutBytes += bytes;
            Get(type).AddOut(copies, bytes);
        }

        public void RecordIn(Type type, int copies, int bytesPerCopy)
        {
            long bytes = (long)bytesPerCopy * copies;
            InMessages += copies;
            InBytes += bytes;
            Get(type).AddIn(copies, bytes);
        }
    }
}
