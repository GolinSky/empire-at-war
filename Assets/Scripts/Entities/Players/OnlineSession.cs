namespace EmpireAtWar.Models.Players
{
    /// <summary>Online battle: the host is the authority; a client only renders and sends commands.</summary>
    public sealed class OnlineSession : IMatchSession
    {
        public OnlineSession(bool isAuthority, PlayerId localPlayerId)
        {
            IsAuthority = isAuthority;
            LocalPlayerId = localPlayerId;
        }

        public bool IsOnline => true;
        public bool IsAuthority { get; }
        public PlayerId LocalPlayerId { get; }
    }
}
