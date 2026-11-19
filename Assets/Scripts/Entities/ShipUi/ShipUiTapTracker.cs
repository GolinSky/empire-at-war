using System;
using UnityEngine;

namespace EmpireAtWar.Entities.ShipUi
{
    /// <summary>Detects a second tap on the same entity within a short window, independent of which button was hit.</summary>
    public sealed class ShipUiTapTracker
    {
        public const float DOUBLE_TAP_SECONDS = 0.3f;

        private readonly Func<float> _clock;
        private long _lastEntityId;
        private float _lastTime;
        private bool _hasTap;

        public ShipUiTapTracker(Func<float> clock = null) => _clock = clock ?? (() => Time.unscaledTime);

        public void Record(long entityId)
        {
            _lastEntityId = entityId;
            _lastTime = _clock();
            _hasTap = true;
        }

        /// <summary>True when this tap completes a double tap; the tracker is cleared in that case.</summary>
        public bool RegisterTap(long entityId)
        {
            bool isDouble = _hasTap && _lastEntityId == entityId && _clock() - _lastTime <= DOUBLE_TAP_SECONDS;
            if (isDouble) Clear();
            else Record(entityId);
            return isDouble;
        }

        public void Clear() => _hasTap = false;
    }
}
