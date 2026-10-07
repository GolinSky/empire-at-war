namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    public readonly struct ProductionCandidate
    {
        public UnitCombatProfile Profile { get; }
        public int Price { get; }

        /// <summary>Units of this type already alive or bought; repeats score lower so the fleet stays varied.</summary>
        public int OwnedCount { get; }

        /// <summary>Whether a carrier's hangar squadrons count toward the candidate's value.</summary>
        public bool IncludeHangar { get; }

        public ProductionCandidate(UnitCombatProfile profile, int price, int ownedCount = 0, bool includeHangar = true)
        {
            Profile = profile;
            Price = price;
            OwnedCount = ownedCount;
            IncludeHangar = includeHangar;
        }
    }
}
