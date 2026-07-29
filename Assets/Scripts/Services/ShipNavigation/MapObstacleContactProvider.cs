using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.ShipNavigation
{
    public interface IMapObstacleContactSource
    {
        RadarContact Contact { get; }
    }

    public interface IMapObstacleContactProvider : IService
    {
        void CopyContacts(List<RadarContact> destination);
    }

    public sealed class MapObstacleContactProvider : Service,
        IMapObstacleContactProvider
    {
        private readonly IReadOnlyList<IMapObstacleContactSource> _sources;

        public MapObstacleContactProvider(
            List<IMapObstacleContactSource> sources)
        {
            _sources = sources ??
                throw new ArgumentNullException(nameof(sources));
        }

        public void CopyContacts(List<RadarContact> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            for (int i = 0; i < _sources.Count; i++)
            {
                IMapObstacleContactSource source = _sources[i];
                if (source == null)
                {
                    throw new InvalidOperationException(
                        "A registered map obstacle contact source is missing.");
                }

                destination.Add(source.Contact);
            }
        }
    }
}
