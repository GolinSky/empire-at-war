using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;

namespace EmpireAtWar.Services.CaptureSites
{
    public interface ICaptureSiteDirectory
    {
        event Action SitesChanged;
        IReadOnlyList<ICaptureSite> Sites { get; }
    }
}
