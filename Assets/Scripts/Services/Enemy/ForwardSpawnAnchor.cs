using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>An own unit a new ship may spawn beside; <see cref="Kind"/> is the telemetry label.</summary>
    public readonly struct ForwardSpawnAnchor
    {
        public Vector3 Position { get; }
        public string Kind { get; }

        public ForwardSpawnAnchor(Vector3 position, string kind)
        {
            Position = position;
            Kind = kind;
        }
    }
}
