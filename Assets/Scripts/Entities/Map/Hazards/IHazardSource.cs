using UnityEngine;

namespace EmpireAtWar.Entities.Map.Hazards
{
    public interface IHazardSource
    {
        SpaceHazardData Data { get; }
        bool Contains(Vector3 position, float footprintRadius, bool wasInside);
    }
}
