using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Flight
{
    public interface ISquadronFlightComponent : IComponent
    {
        IFighterFlightData Data { get; }
        int Count { get; }
        Vector3 Centroid { get; }
        Vector3 Heading { get; }
        bool IsAlive(int index);
        Vector3 GetPosition(int index);
        Vector3 GetForward(int index);
        void Steer(int index, Vector3 target, float speed);
        void Step(float deltaTime);
    }
}
