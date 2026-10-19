using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    /// <summary>How a ship enters the battle: a hyperspace jump facing the enemy, or already in place (a loaded save).</summary>
    public sealed class ShipSpawnPose
    {
        public static readonly ShipSpawnPose Hyperspace = new ShipSpawnPose(false, Quaternion.identity);

        public bool IsRestored { get; }
        /// <summary>Only meaningful when <see cref="IsRestored"/>.</summary>
        public Quaternion Rotation { get; }

        private ShipSpawnPose(bool isRestored, Quaternion rotation)
        {
            IsRestored = isRestored;
            Rotation = rotation;
        }

        public static ShipSpawnPose Restored(Quaternion rotation) => new ShipSpawnPose(true, rotation);
    }
}
