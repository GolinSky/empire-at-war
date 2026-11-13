using EmpireAtWar.Entities.Planet;

namespace EmpireAtWar.Entities.Map
{
    public static class MapPlanetSeed
    {
        public static int FromPlanet(PlanetType planet)
        {
            // Numeric enum identities are serialized and stable across runtimes; string hashes are not.
            unchecked
            {
                uint value = (uint)planet + 0x9E3779B9u;
                value = (value ^ (value >> 16)) * 0x85EBCA6Bu;
                value = (value ^ (value >> 13)) * 0xC2B2AE35u;
                return (int)(value ^ (value >> 16));
            }
        }
    }
}
