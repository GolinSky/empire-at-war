namespace EmpireAtWar.MiningFacility
{
    /// <summary>Mining facility built on a captured asteroid site; loads the Asteroid-prefixed view and data.</summary>
    public class AsteroidMiningFacilityInstaller : MiningFacilityInstaller
    {
        private const string ASTEROID_PREFIX = "Asteroid";

        protected override string ModelPathPrefix => ASTEROID_PREFIX;
        protected override string PrefabPathPrefix => ASTEROID_PREFIX;
    }
}
