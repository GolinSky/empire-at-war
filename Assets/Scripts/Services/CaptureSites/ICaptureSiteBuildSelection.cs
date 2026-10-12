using System;
using EmpireAtWar.Entities.CaptureSites;
using UnityEngine;

namespace EmpireAtWar.Services.CaptureSites
{
    public interface ICaptureSiteBuildSelection
    {
        event Action SelectionChanged;
        CaptureSitePresenter SelectedSite { get; }
        Vector2 SelectionScreenPosition { get; }
        bool CanAfford(float price);
        void BuildSelected(SiteFacilityType facilityType);
    }
}
