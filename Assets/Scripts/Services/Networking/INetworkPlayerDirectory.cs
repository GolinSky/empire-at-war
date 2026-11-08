using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Host: which player slot each Mirror connection controls (filled by the lobby, Phase 2).</summary>
    public interface INetworkPlayerDirectory
    {
        bool TryGetPlayer(int connectionId, out PlayerId player);
    }
}
