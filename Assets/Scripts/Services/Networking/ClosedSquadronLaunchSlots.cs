using EmpireAtWar.Services.Squadrons;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Online client: hangars never launch locally; the host's squadrons arrive as spawns.</summary>
    public sealed class ClosedSquadronLaunchSlots : ISquadronLaunchSlots
    {
        public bool IsAvailable => false;

        public void Take() => throw new System.InvalidOperationException("Clients never launch squadrons.");
    }
}
