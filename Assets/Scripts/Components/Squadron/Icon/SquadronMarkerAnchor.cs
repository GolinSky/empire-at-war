using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>Low-passes the fighter centroid; the icon follows the squadron without cruise-speed or tether lag.</summary>
    internal sealed class SquadronMarkerAnchor
    {
        private const float TRAVEL_SMOOTH_TIME = 0.15f;
        // Fighters circling a target make the centroid wobble; a slow filter hides it while the squadron is not travelling.
        private const float SETTLED_SMOOTH_TIME = 0.8f;
        private const float TELEPORT_DISTANCE = 150f;
        private Vector3 _lastCentroid;
        private Vector3 _velocity;

        public Vector3 Position { get; private set; }

        public void Reset(Vector3 centroid)
        {
            Position = centroid;
            _lastCentroid = centroid;
            _velocity = Vector3.zero;
        }

        /// <returns>True when the centroid teleported (spawn, respawn, replica snap) and the anchor was reset.</returns>
        public bool Advance(Vector3 centroid, bool travelling, float deltaTime)
        {
            if ((centroid - _lastCentroid).sqrMagnitude > TELEPORT_DISTANCE * TELEPORT_DISTANCE)
            {
                Reset(centroid);
                return true;
            }

            _lastCentroid = centroid;
            Position = Vector3.SmoothDamp(Position, centroid, ref _velocity,
                travelling ? TRAVEL_SMOOTH_TIME : SETTLED_SMOOTH_TIME, Mathf.Infinity, deltaTime);
            return false;
        }
    }
}
