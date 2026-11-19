using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Services.ShipNavigation;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>A map without obstacles: every pace-cluster link segment is clear.</summary>
    public sealed class EmptyObstacles : IMapObstacleContactProvider
    {
        public string Id => nameof(EmptyObstacles);
        public void CopyContacts(List<RadarContact> destination) { }
    }
}
