using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>
    /// Network status overlay state: available only in online battles; the cheat panel toggles visibility;
    /// the collector publishes the latest lines once per second.
    /// </summary>
    public sealed class NetworkStatusModel
    {
        private IReadOnlyList<string> _lines = Array.Empty<string>();

        public NetworkStatusModel(bool isAvailable) => IsAvailable = isAvailable;

        public bool IsAvailable { get; }
        public bool IsVisible { get; private set; }
        public IReadOnlyList<string> Lines => _lines;

        public void SetVisible(bool visible) => IsVisible = visible && IsAvailable;

        public void Publish(IReadOnlyList<string> lines) => _lines = lines;
    }
}
