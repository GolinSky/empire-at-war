using System;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Entities.CaptureSites
{
    public interface ICaptureSiteView
    {
        event Action BuildPressed;

        Vector3 Center { get; }
        float Radius { get; }
        float CaptureDuration { get; }
        SiteFacilityType FacilityType { get; }

        void Render(
            PlayerType owner,
            CaptureSiteState state,
            PlayerType capturingPlayer,
            float captureProgress,
            float constructionProgress,
            bool isContested);
        void SetVisibility(bool isVisible, bool showStatus);
        void SetBuildOption(bool isVisible, bool isInteractable, string label);
    }
}
