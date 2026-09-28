using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Models.Players
{
    /// <summary>One participant of a skirmish as chosen in the setup screen; immutable for the whole match.</summary>
    public sealed class PlayerSlot
    {
        public PlayerSlot(
            PlayerId id,
            TeamId team,
            FactionType faction,
            PlayerController controller,
            EnemyAiDifficulty difficulty,
            int colorIndex)
        {
            Id = id;
            Team = team;
            Faction = faction;
            Controller = controller;
            Difficulty = difficulty;
            ColorIndex = colorIndex;
        }

        public PlayerId Id { get; }
        public TeamId Team { get; }
        public FactionType Faction { get; }
        public PlayerController Controller { get; }
        /// <summary>Only meaningful for AI players.</summary>
        public EnemyAiDifficulty Difficulty { get; }
        /// <summary>Index into the match team-color palette.</summary>
        public int ColorIndex { get; }
        public bool IsAi => Controller == PlayerController.Ai;
    }
}
