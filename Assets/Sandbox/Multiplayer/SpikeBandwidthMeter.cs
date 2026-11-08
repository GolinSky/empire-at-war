using System;
using Mirror;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Message-level traffic (Mirror NetworkDiagnostics; excludes transport headers), averaged per second with peak.
    public sealed class SpikeBandwidthMeter : IDisposable
    {
        private const double WINDOW_SECONDS = 1.0;

        private long _perClientBytes;
        private long _uploadBytes;
        private long _inBytes;
        private double _windowStart;

        public SpikeBandwidthMeter(double now)
        {
            _windowStart = now;
            NetworkDiagnostics.OutMessageEvent += OnOut;
            NetworkDiagnostics.InMessageEvent += OnIn;
        }

        // Host: bytes/s sent to one client. Client: bytes/s uploaded.
        public double PerClientBytesPerSecond { get; private set; }
        public double PeakPerClientBytesPerSecond { get; private set; }
        // Host: bytes/s sent to all remote connections.
        public double UploadBytesPerSecond { get; private set; }
        public double InBytesPerSecond { get; private set; }
        public double PeakInBytesPerSecond { get; private set; }

        public void Update(double now)
        {
            double elapsed = now - _windowStart;
            if (elapsed < WINDOW_SECONDS)
                return;

            PerClientBytesPerSecond = _perClientBytes / elapsed;
            UploadBytesPerSecond = _uploadBytes / elapsed;
            InBytesPerSecond = _inBytes / elapsed;
            PeakPerClientBytesPerSecond = Math.Max(PeakPerClientBytesPerSecond, PerClientBytesPerSecond);
            PeakInBytesPerSecond = Math.Max(PeakInBytesPerSecond, InBytesPerSecond);
            _perClientBytes = 0;
            _uploadBytes = 0;
            _inBytes = 0;
            _windowStart = now;
        }

        public void ResetPeaks()
        {
            PeakPerClientBytesPerSecond = 0;
            PeakInBytesPerSecond = 0;
        }

        public void Dispose()
        {
            NetworkDiagnostics.OutMessageEvent -= OnOut;
            NetworkDiagnostics.InMessageEvent -= OnIn;
        }

        private void OnOut(NetworkDiagnostics.MessageInfo info)
        {
            int remoteRecipients = NetworkServer.active && NetworkServer.localConnection != null ? info.count - 1 : info.count;
            if (remoteRecipients <= 0)
                return;

            _perClientBytes += info.bytes;
            _uploadBytes += (long)info.bytes * remoteRecipients;
        }

        private void OnIn(NetworkDiagnostics.MessageInfo info) => _inBytes += (long)info.bytes * info.count;
    }
}
