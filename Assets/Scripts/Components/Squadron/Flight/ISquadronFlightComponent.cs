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
        /// <summary>Centroid of the live fighters. Equals <see cref="Centroid"/> except on online clients, whose root holds the host's pose.</summary>
        Vector3 FighterCentroid { get; }

        bool IsAlive(int index);

        Vector3 GetPosition(int index);

        Vector3 GetForward(int index);

        void Steer(int index, Vector3 target, float speed);

        void Step(float deltaTime);

        /// <summary>Moves every live fighter by <paramref name="offset"/>; their poses follow on the next <see cref="Step"/>.</summary>
        void Shift(Vector3 offset);

        /// <summary>Limits every fighter's speed to a group pace; PositiveInfinity removes the cap.</summary>
        void SetSpeedCap(float cap);

        /// <summary>Spawns the fighters again in formation at the launch point of a reused squadron.</summary>
        void Respawn(Vector3 position, Quaternion rotation);
    }
}
