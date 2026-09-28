namespace EmpireAtWar.Models.Players
{
    /// <summary>
    /// The single place that decides whether two owners fight each other.
    /// Never compare owners with == / != to decide hostility.
    /// </summary>
    public interface IPlayerRelations
    {
        /// <summary>True when both owners are real players on different teams.</summary>
        bool IsHostile(PlayerId first, PlayerId second);

        /// <summary>True when both owners are real players on the same team, including a player with itself.</summary>
        bool IsAllied(PlayerId first, PlayerId second);
    }
}
