namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    public readonly struct ProductionCandidate
    {
        public UnitCombatProfile Profile { get; }
        public int Price { get; }

        public ProductionCandidate(UnitCombatProfile profile, int price)
        {
            Profile = profile;
            Price = price;
        }
    }
}
