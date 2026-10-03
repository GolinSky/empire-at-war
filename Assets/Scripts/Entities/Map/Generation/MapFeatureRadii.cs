namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Footprint radii of the spawned zone and site prefabs, which own these values.</summary>
    public readonly struct MapFeatureRadii
    {
        public float Zone { get; }
        public float MiningSite { get; }
        public float BattleSite { get; }

        public MapFeatureRadii(float zone, float miningSite, float battleSite)
        {
            Zone = zone;
            MiningSite = miningSite;
            BattleSite = battleSite;
        }
    }
}
