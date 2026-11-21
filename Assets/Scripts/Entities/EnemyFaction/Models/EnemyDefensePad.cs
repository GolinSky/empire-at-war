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

        /// <summary>Satellite pads of the pad's point, whoever owns them.</summary>
        public int PointPads { get; }

        /// <summary>A team station, platform, mine or satellite stands within repair-satellite reach of the pad.</summary>
        public bool HasTeamStructureInRepairRange { get; }

        /// <summary>The point is a lone lane pad: no mine, no other pad. Filled last, never required.</summary>
        public bool IsFreePad { get; }

        /// <summary>The pad stands beside an own station.</summary>
        public bool IsHomePad { get; }

        public EnemyDefensePad(int siteIndex, float protectedMineShare, float exposure, float secondsSinceHostile,
            bool hasHostiles, bool isThreatened, IReadOnlyList<SiteFacilityType> existing, bool isZonePad = false,
            bool isCentrePad = false, int pointPads = 1, bool hasTeamStructureInRepairRange = false,
            bool isFreePad = false, bool isHomePad = false)
        {
            IsFreePad = isFreePad;
            IsHomePad = isHomePad;
            PointPads = pointPads;
            HasTeamStructureInRepairRange = hasTeamStructureInRepairRange;
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
