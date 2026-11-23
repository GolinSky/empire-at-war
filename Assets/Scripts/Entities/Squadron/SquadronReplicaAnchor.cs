using EmpireAtWar.Components.Squadrons.Flight;
using UnityEngine;

namespace EmpireAtWar.Entities.Squadrons
{
    /// <summary>
    /// Online client: the host flies the squadron and only its root pose is replicated. Keeps the local fighters on
    /// that root: they fly a wedge along its course or orbit it, a replayed attack swarms its target, and fighters
    /// that drift from the root are pulled back, or moved at once after a jump such as a launch or a respawn.
    /// </summary>
    public sealed class SquadronReplicaAnchor
    {
        // A root slower than this share of cruise speed is orbited instead of followed in a wedge.
        private const float MOVING_SPEED_SHARE = 0.25f;
        private const float VELOCITY_SMOOTH_TIME = 0.3f;
        private const float PULL_TIME = 0.75f;
        private const float MAX_PULL_SPEED_SHARE = 0.5f;
        // Drift beyond this many loiter radii is a jump, not flight.
        private const float SNAP_LOITER_RADII = 10f;

        private readonly ISquadronFlightComponent _flight;
        private readonly SquadronPilot _pilot;
        private Vector3 _lastRoot;
        private Vector3 _velocity;
        private bool _hasRoot;

        public SquadronReplicaAnchor(ISquadronFlightComponent flight, SquadronPilot pilot)
        {
            _flight = flight;
            _pilot = pilot;
        }

        /// <summary>Forgets the previous life's root motion.</summary>
        public void Reset()
        {
            _hasRoot = false;
            _velocity = Vector3.zero;
        }

        /// <summary>Runs before the pilot ticks, while the squadron root still holds the host's pose.</summary>
        public void Steer(Vector3 hostRoot, float deltaTime)
        {
            if (deltaTime <= 0f) return;
            float snapDistance = _flight.Data.LoiterRadius * SNAP_LOITER_RADII;
            TrackVelocity(hostRoot, deltaTime, snapDistance);
            Pull(hostRoot, deltaTime, snapDistance);
            if (_pilot.IsFighting) return;

            float speed = _velocity.magnitude;
            if (speed > _flight.Data.CruiseSpeed * MOVING_SPEED_SHARE)
                _pilot.Follow(hostRoot, _velocity / speed, speed);
            else
                _pilot.Orbit(hostRoot);
        }

        private void TrackVelocity(Vector3 hostRoot, float deltaTime, float snapDistance)
        {
            Vector3 moved = hostRoot - _lastRoot;
            moved.y = 0f;
            if (!_hasRoot || moved.sqrMagnitude > snapDistance * snapDistance)
                _velocity = Vector3.zero;
            else
                _velocity = Vector3.Lerp(_velocity, moved / deltaTime, 1f - Mathf.Exp(-deltaTime / VELOCITY_SMOOTH_TIME));
            _lastRoot = hostRoot;
            _hasRoot = true;
        }

        private void Pull(Vector3 hostRoot, float deltaTime, float snapDistance)
        {
            Vector3 error = hostRoot - _flight.FighterCentroid;
            error.y = 0f;
            float distance = error.magnitude;
            if (distance > snapDistance)
            {
                _flight.Shift(error);
                // The wedge leader stays where the fighters were; restart from the new position.
                if (!_pilot.IsFighting) _pilot.Loiter(hostRoot);
                return;
            }

            // Inside the loiter radius the pilot decides: an orbiting group's centroid sits up to one radius off
            // its centre, and wedge depth and attack passes stay within it too.
            float excess = distance - _flight.Data.LoiterRadius;
            if (excess <= 0f) return;
            float step = Mathf.Min(excess * (1f - Mathf.Exp(-deltaTime / PULL_TIME)),
                _flight.Data.CruiseSpeed * MAX_PULL_SPEED_SHARE * deltaTime);
            _flight.Shift(error * (step / distance));
        }
    }
}
