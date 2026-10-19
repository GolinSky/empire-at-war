using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Services.CaptureSites;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// Capture-site ownership, capture and construction progress. An operational site is linked to its restored
    /// facility again, so losing the facility frees the site as before.
    /// </summary>
    public sealed class CaptureSitesSaveSource : IBattleSaveSource
    {
        private readonly CaptureSitesSystem _sites;

        public CaptureSitesSaveSource(CaptureSitesSystem sites)
        {
            _sites = sites;
        }

        public void Capture(BattleSave save) => save.CaptureSites = _sites.SitePresenters.Select(site =>
            CaptureSiteSave.Create(site.Owner, site.State, site.CapturingPlayer, site.CaptureProgress,
                site.FacilityType, site.ConstructionProgress, site.FacilityId)).ToArray();

        public void RestoreBeforeUnits(BattleSave save)
        {
        }

        public void RestoreAfterUnits(BattleRestoreContext context)
        {
            IReadOnlyList<CaptureSitePresenter> sites = _sites.SitePresenters;
            CaptureSiteSave[] saves = context.Save.CaptureSites;
            if (saves.Length != sites.Count)
                throw new InvalidOperationException($"Saved {saves.Length} capture sites; the map has {sites.Count}.");
            for (int i = 0; i < sites.Count; i++)
            {
                CaptureSitePresenter site = sites[i];
                CaptureSiteSave save = saves[i];
                site.Restore(save.Owner, save.State, save.CapturingPlayer, save.CaptureProgress, save.FacilityType,
                    save.ConstructionProgress);
                if (save.State != CaptureSiteState.Operational) continue;
                site.SetFacility(save.FacilityId);
                if (context.MiningFacilities.TryGetValue(save.FacilityId, out var facility))
                    facility.OnRelease += site.ReleaseFacility;
                else
                    context.DefendPlatforms[save.FacilityId].OnRelease += site.ReleaseFacility;
            }
        }
    }
}
