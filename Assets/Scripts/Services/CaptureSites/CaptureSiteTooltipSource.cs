using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Tooltip;
using UnityEngine;

namespace EmpireAtWar.Services.CaptureSites
{
    public sealed class CaptureSiteTooltipSource : IWorldTooltipSource
    {
        private readonly ICaptureSiteDirectory _sites;
        private readonly IHoveredEntityObserver _hover;
        private readonly CaptureSiteData _data;
        private readonly PlayerDisplayNames _playerNames;

        public CaptureSiteTooltipSource(ICaptureSiteDirectory sites, IHoveredEntityObserver hover,
            CaptureSiteData data, PlayerDisplayNames playerNames)
        {
            _sites = sites;
            _hover = hover;
            _data = data;
            _playerNames = playerNames;
        }

        public bool TryGetTooltip(Ray ray, out ITooltipContentProvider provider)
        {
            ICaptureSite site = CaptureSitePointerQuery.Find(_sites.Sites, ray);
            if (site != null)
            {
                provider = new TooltipContentProvider(this, site,
                    () => _hover.CanHover && site.IsRevealed, () => BuildContent(site, _data, _playerNames));
                return true;
            }

            provider = null;
            return false;
        }

        internal static TooltipContent BuildContent(ICaptureSite site, CaptureSiteData data, PlayerDisplayNames playerNames)
        {
            string owner = site.Owner.IsNone ? "Neutral" : playerNames.Get(site.Owner);
            bool hasFacility = site.State is CaptureSiteState.Constructing or CaptureSiteState.Operational;
            string status = hasFacility ? $"{site.State} · {data.GetCost(site.FacilityType).Name}" : "Empty";
            return new TooltipContent(title: site.Kind == CaptureSiteKind.Satellite ? "Satellite site" : "Capture site",
                description: $"Bring ships or squadrons within capture range. Each squadron contributes {data.SquadronCaptureWeight:P0} of a ship's capture strength. " +
                    "Select a friendly empty site to choose a facility.",
                stats: new[]
                {
                    new TooltipStat(label: "Capture (%)", current: site.CaptureProgress * 100f, format: "0"),
                    new TooltipStat(label: "Construction (%)", current: site.ConstructionProgress * 100f, format: "0")
                },
                status: $"Owner: {owner} · {status}");
        }
    }
}
