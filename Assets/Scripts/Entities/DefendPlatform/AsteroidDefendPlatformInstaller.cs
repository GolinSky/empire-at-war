namespace EmpireAtWar
{
    /// <summary>Battle asteroid built on a captured site; loads the Asteroid-prefixed view and data.</summary>
    public class AsteroidDefendPlatformInstaller : DefendPlatformInstaller
    {
        private const string ASTEROID_PREFIX = "Asteroid";

        protected override string DataPath => ASTEROID_PREFIX + base.DataPath;
        protected override string PrefabPath => ASTEROID_PREFIX + base.PrefabPath;
    }
}
