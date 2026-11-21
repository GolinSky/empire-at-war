using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    /// <summary>
    /// Engine exhaust trails. Trail length = speed × time, so a lifetime of
    /// 14 × width / undamaged pace gives 14 × width at full pace, shorter when slow.
    /// The lifetime cap keeps slow capitals' trails reshaping quickly in turns.
    /// </summary>
    internal sealed class ShipEnginePlumes
    {
        private const float LENGTH_PER_WIDTH = 14f;
        private const float MAX_LIFETIME = 1.5f;
        private const int SEGMENTS_PER_TRAIL = 9;

        private readonly TrailRenderer[] _plumes;

        public ShipEnginePlumes(TrailRenderer[] plumes, float undamagedSpeed)
        {
            _plumes = plumes;
            foreach (TrailRenderer plume in plumes)
            {
                plume.time = Mathf.Min(LENGTH_PER_WIDTH * plume.widthMultiplier / undamagedSpeed, MAX_LIFETIME);
                plume.minVertexDistance = undamagedSpeed * plume.time / SEGMENTS_PER_TRAIL;
            }
        }

        /// <summary>Hides the trails and drops their points (spawn, hyperspace jump, release).</summary>
        public void Stop()
        {
            foreach (TrailRenderer plume in _plumes)
            {
                plume.emitting = false;
                plume.Clear();
                plume.enabled = false;
            }
        }

        /// <summary>Starts fresh trails at the current position, discarding anything recorded before.</summary>
        public void Start()
        {
            foreach (TrailRenderer plume in _plumes)
            {
                plume.Clear();
                plume.enabled = true;
                plume.emitting = true;
            }
        }
    }
}
