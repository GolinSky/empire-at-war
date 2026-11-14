using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Entities.SpaceStation
{
    public interface IStationHullFacade : IEntityFacade
    {
        float HullBottom { get; }
        float HullTop { get; }

        /// <summary>Planar distance from <paramref name="center"/> that covers the whole hull box.</summary>
        float GetHullRadius(Vector3 center);
    }
}
