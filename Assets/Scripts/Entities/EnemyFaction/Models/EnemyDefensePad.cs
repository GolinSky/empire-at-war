using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public readonly struct EnemyDefensePad
    {
        public int SiteIndex { get; }
        public float ProtectedIncome { get; }
        public float Exposure { get; }
        public float SecondsSinceHostile { get; }
        public bool HasHostiles { get; }
        public bool IsThreatened { get; }
        public IReadOnlyList<SiteFacilityType> Existing { get; }

        public EnemyDefensePad(int siteIndex, float protectedIncome, float exposure, float secondsSinceHostile,
            bool hasHostiles, bool isThreatened, IReadOnlyList<SiteFacilityType> existing)
        {
            SiteIndex = siteIndex;
            ProtectedIncome = protectedIncome;
            Exposure = exposure;
            SecondsSinceHostile = secondsSinceHostile;
            HasHostiles = hasHostiles;
            IsThreatened = isThreatened;
            Existing = existing;
        }
    }
}
