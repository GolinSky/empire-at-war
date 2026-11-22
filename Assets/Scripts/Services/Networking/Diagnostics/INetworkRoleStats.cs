using System.Collections.Generic;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>Host- or client-only network numbers, added to the shared columns once per window.</summary>
    public interface INetworkRoleStats
    {
        /// <summary>
        /// Sets this role's columns (same names and order every window), adds overlay detail lines and one
        /// <see cref="NetworkLinkSample"/> per connection (overlay line and links CSV row).
        /// </summary>
        void Collect(NetworkStatsRow row, List<string> details, List<NetworkLinkSample> links, NetworkStatsWindow window);
    }
}
