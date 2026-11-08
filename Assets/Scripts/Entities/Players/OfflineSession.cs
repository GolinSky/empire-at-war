namespace EmpireAtWar.Models.Players
{
    /// <summary>Single-machine battle: authority with no transport. Battle Simulation has no local player (None).</summary>
    public sealed class OfflineSession : IMatchSession
    {
        public OfflineSession(PlayerId localPlayerId)
        {
            LocalPlayerId = localPlayerId;
        }

        /// <summary>Skirmish/Survival offline: the line-up's single human; throws when there is none or more than one.</summary>
        public static OfflineSession ForHuman(IPlayerRoster playerRoster) =>
            new OfflineSession(MatchRules.FindHuman(playerRoster.Players).Id);

        public bool IsOnline => false;
        public bool IsAuthority => true;
        public PlayerId LocalPlayerId { get; }
    }
}
