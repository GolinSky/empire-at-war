using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    /// <summary>
    /// Engine exhaust trails. Trail length = speed × time, so a lifetime of
    /// 14 × width / undamaged pace gives 14 × width at full pace, shorter when slow.
    /// </summary>
    internal sealed class ShipEnginePlumes
    {
        private const float LENGTH_PER_WIDTH = 14f;
        private const int SEGMENTS_PER_TRAIL = 9;

        private readonly TrailRenderer[] _plumes;

        public ShipEnginePlumes(TrailRenderer[] plumes, float undamagedSpeed)
        {
            _plumes = plumes;
            foreach (TrailRenderer plume in plumes)
            {
                float fullLength = LENGTH_PER_WIDTH * plume.widthMultiplier;
                plume.time = fullLength / undamagedSpeed;
                plume.minVertexDistance = fullLength / SEGMENTS_PER_TRAIL;
            }
        }

        public void SetEmitting(bool isEmitting)
        {
            foreach (TrailRenderer plume in _plumes) plume.emitting = isEmitting;
        }

        public void Clear()
        {
            foreach (TrailRenderer plume in _plumes) plume.Clear();
        }
    }
}
