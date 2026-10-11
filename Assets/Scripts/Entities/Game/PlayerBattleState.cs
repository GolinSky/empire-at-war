using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    /// <summary>What the victory rules need to know about one player this frame.</summary>
    public readonly struct PlayerBattleState
    {
        public PlayerId Player { get; }
        public TeamId Team { get; }
        /// <summary>Living ships, squadrons and structures other than the player base.</summary>
        public int UnitCount { get; }
        public bool IsBaseAlive { get; }
        public bool HasPendingReinforcement { get; }

        public PlayerBattleState(
            PlayerId player,
            TeamId team,
            int unitCount,
            bool isBaseAlive,
            bool hasPendingReinforcement)
        {
            Player = player;
            Team = team;
            UnitCount = unitCount;
            IsBaseAlive = isBaseAlive;
            HasPendingReinforcement = hasPendingReinforcement;
        }
    }
}
