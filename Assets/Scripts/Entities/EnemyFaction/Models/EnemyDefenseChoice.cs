using EmpireAtWar.Entities.CaptureSites;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public readonly struct EnemyDefenseChoice
    {
        public int SiteIndex { get; }
        public SiteFacilityType Type { get; }
        public float Score { get; }
        public float Cost { get; }
        public string Reason { get; }

        public EnemyDefenseChoice(int siteIndex, SiteFacilityType type, float score, float cost, string reason)
        {
            SiteIndex = siteIndex;
            Type = type;
            Score = score;
            Cost = cost;
            Reason = reason;
        }
    }
}
