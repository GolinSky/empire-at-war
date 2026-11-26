namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>Where a strike group pulls back to, in order of preference.</summary>
    public enum EnemyStrikeFallbackKind
    {
        /// <summary>Inside the repair radius of an own or allied repairer.</summary>
        Repairer = 0,
        /// <summary>Behind the own station, on the far side from the threat.</summary>
        Station = 1,
        /// <summary>The main fleet body, when the group is not the main fleet.</summary>
        MainBody = 2,
        /// <summary>A held zone or home.</summary>
        HeldZone = 3
    }
}
