using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.CaptureSites
{
    public interface ICaptureSiteView
    {
        TooltipHoverView TooltipHover { get; }

        Vector3 Center { get; }
        float Radius { get; }
        float CaptureDuration { get; }
        Vector3 FacilityPosition { get; }

        void Render(
            OwnerRelation owner,
            CaptureSiteState state,
            SiteFacilityType facilityType,
            string facilityName,
            OwnerRelation capturer,
            float captureProgress,
            float constructionProgress,
            bool isContested);

        void SetVisibility(bool isVisible, bool showStatus);
    }
}
