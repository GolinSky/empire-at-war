namespace EmpireAtWar
{
    /// <summary>Battle asteroid built on a captured site; loads the Asteroid-prefixed view and data.</summary>
    public class AsteroidDefendPlatformInstaller : DefendPlatformInstaller
    {
        private const string ASTEROID_PREFIX = "Asteroid";

        protected override string ModelPathPrefix => ASTEROID_PREFIX;
        protected override string PrefabPathPrefix => ASTEROID_PREFIX;
    }
}
