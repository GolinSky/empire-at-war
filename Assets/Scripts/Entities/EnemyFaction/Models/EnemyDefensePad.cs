using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public readonly struct EnemyDefensePad
    {
        public int SiteIndex { get; }
        /// <summary>Operational own mines in the point as a share of its mine slots (0–1).</summary>
        public float ProtectedMineShare { get; }
        public float Exposure { get; }
        public float SecondsSinceHostile { get; }
        public bool HasHostiles { get; }
        public bool IsThreatened { get; }
        public IReadOnlyList<SiteFacilityType> Existing { get; }

        /// <summary>The point has no mine sites: a relay or centre zone, held by satellites alone.</summary>
        public bool IsZonePad { get; }
        public bool IsCentrePad { get; }

        public EnemyDefensePad(int siteIndex, float protectedMineShare, float exposure, float secondsSinceHostile,
            bool hasHostiles, bool isThreatened, IReadOnlyList<SiteFacilityType> existing, bool isZonePad = false,
            bool isCentrePad = false)
        {
            IsZonePad = isZonePad || isCentrePad;
            IsCentrePad = isCentrePad;
            SiteIndex = siteIndex;
            ProtectedMineShare = protectedMineShare;
            Exposure = exposure;
            SecondsSinceHostile = secondsSinceHostile;
            HasHostiles = hasHostiles;
            IsThreatened = isThreatened;
            Existing = existing;
        }
    }
}
