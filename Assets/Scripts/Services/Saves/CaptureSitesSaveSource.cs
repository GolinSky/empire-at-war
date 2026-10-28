using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Services.CaptureSites;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// Capture-site ownership, capture and construction progress. An occupied site is linked to its restored
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
            if (save.CaptureSites.Length != _sites.SitePresenters.Count)
                throw new InvalidOperationException(
                    $"Saved {save.CaptureSites.Length} capture sites; the map has {_sites.SitePresenters.Count}.");
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
                if (!site.HasFacility) continue;
                site.SetFacility(save.FacilityId);
                if (context.MiningFacilities.TryGetValue(save.FacilityId, out var facility))
                {
                    facility.OnRelease += site.ReleaseFacility;
                    facility.ConstructionProgressChanged += site.SetConstructionProgress;
                    site.SetConstructionProgress(facility.ConstructionProgress);
                }
                else
                {
                    var platform = context.DefendPlatforms[save.FacilityId];
                    platform.OnRelease += site.ReleaseFacility;
                    platform.ConstructionProgressChanged += site.SetConstructionProgress;
                    site.SetConstructionProgress(platform.ConstructionProgress);
                }
            }
        }
    }
}
