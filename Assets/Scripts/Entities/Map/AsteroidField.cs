using System.Collections.Generic;

namespace EmpireAtWar.Entities.Map
{
    /// <summary>
    /// A readable asteroid area that blocks every ship as one volume; its rocks are decoration only.
    /// </summary>
    public sealed class AsteroidField
    {
        public AsteroidField(IReadOnlyList<FieldVolume> volumes, IReadOnlyList<AsteroidSpot> rocks)
        {
            Volumes = volumes;
            Rocks = rocks;
        }

        public IReadOnlyList<FieldVolume> Volumes { get; }
        public IReadOnlyList<AsteroidSpot> Rocks { get; }
    }
}
