using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host side: sends a keyframe only when the clients' prediction (from the last quantized keyframe) drifts past
    /// the position/heading thresholds, or the refresh interval passes (1 s moving, 3 s idle) to recover from loss.
    /// </summary>
    public sealed class DeadReckoningSender
    {
        private readonly Dictionary<ushort, EntityMotion> _lastSent = new Dictionary<ushort, EntityMotion>();
        private readonly Dictionary<ushort, double> _lastSentTime = new Dictionary<ushort, double>();

        /// <summary>Returns true and records the keyframe when <paramref name="actual"/> must be sent at <paramref name="now"/>.</summary>
        public bool TryCreateKeyframe(ushort index, EntityMotion actual, double now, out EntityKeyframe keyframe)
        {
            keyframe = default;
            if (_lastSent.TryGetValue(index, out EntityMotion sent))
            {
                float age = (float)(now - _lastSentTime[index]);
                EntityMotion predicted = sent.Extrapolate(age);
                bool moving = actual.Speed > 0f || sent.Speed > 0f;
                float refresh = moving ? NetworkTickSettings.MOVING_REFRESH_SECONDS : NetworkTickSettings.IDLE_REFRESH_SECONDS;
                bool drifted = Vector3.Distance(predicted.Position, actual.Position) > NetworkTickSettings.POSITION_ERROR_THRESHOLD ||
                               Mathf.Abs(Mathf.DeltaAngle(predicted.Heading, actual.Heading)) > NetworkTickSettings.HEADING_ERROR_THRESHOLD;
                if (!drifted && age < refresh) return false;
            }

            keyframe = EntityKeyframeCodec.Encode(index, actual);
            _lastSent[index] = EntityKeyframeCodec.Decode(keyframe);
            _lastSentTime[index] = now;
            return true;
        }

        /// <summary>A joining or reconnecting client needs every entity once.</summary>
        public void ForceFullResend()
        {
            _lastSent.Clear();
            _lastSentTime.Clear();
        }

        public void Forget(ushort index)
        {
            _lastSent.Remove(index);
            _lastSentTime.Remove(index);
        }
    }
}
