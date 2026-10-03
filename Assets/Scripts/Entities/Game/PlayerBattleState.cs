using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    /// <summary>What the victory rules need to know about one player this frame.</summary>
    public readonly struct PlayerBattleState
    {
        public PlayerId Player { get; }
        public TeamId Team { get; }
        public int ShipCount { get; }
        public bool IsBaseAlive { get; }
        public bool HasPendingReinforcement { get; }

        public PlayerBattleState(
            PlayerId player,
            TeamId team,
            int shipCount,
            bool isBaseAlive,
            bool hasPendingReinforcement)
        {
            Player = player;
            Team = team;
            ShipCount = shipCount;
            IsBaseAlive = isBaseAlive;
            HasPendingReinforcement = hasPendingReinforcement;
        }
    }
}
