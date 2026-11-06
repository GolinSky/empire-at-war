using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Services.Battle;

namespace EmpireAtWar.Services.CaptureSites
{
    public sealed class ConstructingSitesSurvival : IBattleSurvivalSource
    {
        private readonly ICaptureSiteDirectory _sites;

        public ConstructingSitesSurvival(ICaptureSiteDirectory sites) => _sites = sites;

        public void AddUnitCounts(int[] countsByPlayer)
        {
            IReadOnlyList<ICaptureSite> sites = _sites.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                ICaptureSite site = sites[i];
                if (site.State == CaptureSiteState.Constructing)
                    countsByPlayer[site.Owner.Index]++;
            }
        }
    }
}
