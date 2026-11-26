namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>One point a strike group could pull back to (<see cref="EnemyStrikeFallbackRules"/>).</summary>
    public readonly struct EnemyStrikeFallbackCandidate
    {
        public EnemyStrikeFallbackKind Kind { get; }

        /// <summary>A force the group cannot beat stands on the way to the point.</summary>
        public bool IsCutOff { get; }

        /// <summary>A known hostile weapon reaches the point.</summary>
        public bool IsInHostileReach { get; }

        public EnemyStrikeFallbackCandidate(EnemyStrikeFallbackKind kind, bool isCutOff, bool isInHostileReach)
        {
            Kind = kind;
            IsCutOff = isCutOff;
            IsInHostileReach = isInHostileReach;
        }
    }
}
