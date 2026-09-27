using System;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Entities.CaptureSites
{
    public interface ICaptureSiteView
    {
        event Action<SiteFacilityType> BuildPressed;

        Vector3 Center { get; }
        float Radius { get; }
        float CaptureDuration { get; }

        void ConfigureOption(SiteFacilityType facilityType, string displayName, string costLabel);
        void Render(
            PlayerType owner,
            CaptureSiteState state,
            SiteFacilityType facilityType,
            PlayerType capturingPlayer,
            float captureProgress,
            float constructionProgress,
            bool isContested);
        void SetVisibility(bool isVisible, bool showStatus);
        void SetBuildOptionsVisible(bool isVisible);
        void SetOptionInteractable(SiteFacilityType facilityType, bool isInteractable);
    }
}
