using System.Collections.Generic;

namespace EmpireAtWar.Entities.Map
{
    /// <summary>
    /// A readable asteroid area that blocks, as one volume, every ship whose hull reaches its height band;
    /// its rocks are decoration only.
    /// </summary>
    public sealed class AsteroidField
    {
        public IReadOnlyList<FieldVolume> Volumes { get; }
        public IReadOnlyList<AsteroidSpot> Rocks { get; }
        public float Floor { get; }
        public float Ceiling { get; }

        public AsteroidField(IReadOnlyList<FieldVolume> volumes, IReadOnlyList<AsteroidSpot> rocks,
            float floor, float ceiling)
        {
            Volumes = volumes;
            Rocks = rocks;
            Floor = floor;
            Ceiling = ceiling;
        }
    }
}
