using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    /// <summary>How a ship enters the battle: a hyperspace jump facing the enemy or a chosen heading,
    /// or already in place (a loaded save).</summary>
    public sealed class ShipSpawnPose
    {
        public static readonly ShipSpawnPose Hyperspace = new ShipSpawnPose(false, false, Quaternion.identity);

        public bool IsRestored { get; }
        /// <summary>False when the ship takes its station's facing.</summary>
        public bool HasRotation { get; }
        /// <summary>Only meaningful when <see cref="HasRotation"/>.</summary>
        public Quaternion Rotation { get; }

        private ShipSpawnPose(bool isRestored, bool hasRotation, Quaternion rotation)
        {
            IsRestored = isRestored;
            HasRotation = hasRotation;
            Rotation = rotation;
        }

        public static ShipSpawnPose Restored(Quaternion rotation) => new ShipSpawnPose(true, true, rotation);

        /// <summary>A hyperspace jump that lands with the given heading.</summary>
        public static ShipSpawnPose HyperspaceFacing(Quaternion rotation) => new ShipSpawnPose(false, true, rotation);
    }
}
