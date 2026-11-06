using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;

namespace EmpireAtWar.Services.CaptureSites
{
    public interface ICaptureSiteSaveState
    {
        IReadOnlyList<CaptureSitePresenter> SitePresenters { get; }
    }
}
