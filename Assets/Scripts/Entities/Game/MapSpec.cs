using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;

namespace EmpireAtWar.Entities.Game
{
    public sealed class MapSpec
    {
        public MapType Type { get; }
        public PlanetType Planet { get; }
        public MapSize Size { get; }

        public MapSpec(MapType type, PlanetType planet, MapSize size)
        {
            Type = type;
            Planet = planet;
            Size = size;
        }
    }
}
