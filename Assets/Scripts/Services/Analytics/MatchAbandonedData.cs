using EmpireAtWar.Entities.Game;

namespace EmpireAtWar.Services.Analytics
{
    public sealed class MatchAbandonedData
    {
        public string MatchId { get; }
        public GameMode Mode { get; }
        public float DurationSeconds { get; }
        public MatchAbandonReason Reason { get; }

        public MatchAbandonedData(string matchId, GameMode mode, float durationSeconds, MatchAbandonReason reason)
        {
            MatchId = matchId;
            Mode = mode;
            DurationSeconds = durationSeconds;
            Reason = reason;
        }
    }
}
