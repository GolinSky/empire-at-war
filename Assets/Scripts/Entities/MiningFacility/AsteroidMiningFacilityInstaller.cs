namespace EmpireAtWar.MiningFacility
{
    /// <summary>Mining facility built on a captured asteroid site; loads the Asteroid-prefixed view and data.</summary>
    public class AsteroidMiningFacilityInstaller : MiningFacilityInstaller
    {
        private const string ASTEROID_PREFIX = "Asteroid";

        protected override string DataPath => ASTEROID_PREFIX + base.DataPath;
        protected override string PrefabPath => ASTEROID_PREFIX + base.PrefabPath;
    }
}
