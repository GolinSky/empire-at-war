using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Tooltip;
using UnityEngine;

namespace EmpireAtWar.Services.CaptureSites
{
    public sealed class CaptureSiteTooltipSource : IWorldTooltipSource
    {
        private readonly ICaptureSiteDirectory _sites;
        private readonly IHoveredEntityObserver _hover;

        public CaptureSiteTooltipSource(ICaptureSiteDirectory sites, IHoveredEntityObserver hover)
        {
            _sites = sites;
            _hover = hover;
        }

        public bool TryGetTooltip(Ray ray, out ITooltipContentProvider provider)
        {
            ICaptureSite site = CaptureSitePointerQuery.Find(_sites.Sites, ray);
            if (site != null)
            {
                provider = new TooltipContentProvider(this, site,
                    () => _hover.CanHover && site.IsRevealed, () => new TooltipContent(title: "Capture site",
                        description: "Move ships into the ring to capture it. Select an owned empty site to construct a facility.",
                        stats: new[] { new TooltipStat(label: "Capture (%)", current: site.CaptureProgress * 100f),
                            new TooltipStat(label: "Construction (%)", current: site.ConstructionProgress * 100f) },
                        status: $"Owner: {site.Owner} · {site.State} · {site.FacilityType}"));
                return true;
            }

            provider = null;
            return false;
        }
    }
}
