namespace EmpireAtWar.Models.Players
{
    /// <summary>How an owner relates to the local player; what views use to pick colors and labels.</summary>
    public enum OwnerRelation
    {
        Neutral = 0,
        Own = 1,
        Ally = 2,
        Enemy = 3
    }
}
