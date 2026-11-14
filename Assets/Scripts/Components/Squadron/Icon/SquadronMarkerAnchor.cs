using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    internal sealed class SquadronMarkerAnchor
    {
        private const float COMBAT_RESPONSE = 0.8f;
        private const float GUARD_RESPONSE = 2.5f;
        private const float VELOCITY_RESPONSE = 8f;
        private Vector3 _lastCentroid;

        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }

        public void Reset(Vector3 centroid)
        {
            Position = centroid;
            _lastCentroid = centroid;
            Velocity = Vector3.zero;
        }

        // The pilot's virtual leader and loiter center already describe the whole squadron's intent.
        // In combat the centroid is filtered instead; individual fighter positions are never queried.
        public bool Advance(Vector3 centroid, Vector3 intent, float cruiseSpeed, float loiterRadius,
            bool travelling, bool fighting, float followSharpness, float deltaTime)
        {
            float tether = loiterRadius * (fighting ? 3f : 2f);
            float discontinuity = 4f * tether + cruiseSpeed * deltaTime * 2f;
            if ((centroid - _lastCentroid).sqrMagnitude > discontinuity * discontinuity)
            {
                Reset(centroid);
                return true;
            }

            _lastCentroid = centroid;
            Vector3 target = fighting ? centroid : intent;
            target.y = centroid.y;
            target = centroid + Vector3.ClampMagnitude(target - centroid, tether);
            float response = travelling ? followSharpness : fighting ? COMBAT_RESPONSE : GUARD_RESPONSE;
            float deadZone = travelling ? 0f : loiterRadius * (fighting ? 1f : 0.15f);
            Vector3 previous = Position;
            Vector3 delta = target - previous;
            float distance = delta.magnitude;
            if (distance > deadZone)
            {
                float step = (distance - deadZone) * (1f - Mathf.Exp(-response * deltaTime));
                Position += delta / distance * Mathf.Min(step, cruiseSpeed * deltaTime);
            }

            Vector3 displacement = Position - centroid;
            if (displacement.sqrMagnitude > tether * tether)
                Position = Vector3.MoveTowards(Position, centroid + Vector3.ClampMagnitude(displacement, tether),
                    cruiseSpeed * deltaTime);

            Velocity = Vector3.Lerp(Velocity, (Position - previous) / deltaTime,
                1f - Mathf.Exp(-VELOCITY_RESPONSE * deltaTime));
            return false;
        }
    }
}
