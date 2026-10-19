using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;

namespace EmpireAtWar.Entities.Game
{
    public sealed class MapSpec
    {
        public MapType Type { get; }
        public PlanetType Planet { get; }
        public MapSize Size { get; }
        /// <summary>Seeds the generated layout; only <see cref="MapType.Full"/> maps use it.</summary>
        public int Seed { get; }

        public MapSpec(MapType type, PlanetType planet, MapSize size, int seed = 0)
        {
            Type = type;
            Planet = planet;
            Size = size;
            Seed = seed;
        }
    }
}
