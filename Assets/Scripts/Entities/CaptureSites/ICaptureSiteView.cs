using System;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.CaptureSites
{
    public interface ICaptureSiteView
    {
        event Action<SiteFacilityType> BuildPressed;

        Vector3 Center { get; }
        float Radius { get; }
        float CaptureDuration { get; }
        Vector3 FacilityPosition { get; }

        void ConfigureOption(SiteFacilityType facilityType, string displayName, string costLabel);

        void Render(
            OwnerRelation owner,
            CaptureSiteState state,
            SiteFacilityType facilityType,
            OwnerRelation capturer,
            float captureProgress,
            float constructionProgress,
            bool isContested);

        void SetVisibility(bool isVisible, bool showStatus);

        void SetBuildOptionsVisible(bool isVisible);

        void SetBuildOptionsPosition(Vector2 screenPosition);

        void SetOptionInteractable(SiteFacilityType facilityType, bool isInteractable);
    }
}
