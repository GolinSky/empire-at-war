using System;
using EmpireAtWar.Entities.CaptureSites;

namespace EmpireAtWar.Services.CaptureSites
{
    public interface ICaptureSiteStatusEvents : ICaptureSiteDirectory
    {
        event Action<CaptureSiteStatusChange> StatusChanged;
    }
}
