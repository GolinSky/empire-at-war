using System;
using UnityEngine;

namespace EmpireAtWar.Services.UnitExplosion
{
    /// <summary>Battle-wide and per-unit limits for hull hit blasts. Tune from the "[VfxPools]" battle log line.</summary>
    [Serializable]
    public sealed class HullBlastLimits
    {
        [Tooltip("Blasts alive at once across the battle (playing and fading). Also the prewarm count: the pool never grows.")]
        [SerializeField, Min(1)] private int maxActive = 24;
        [Tooltip("New blasts per frame across the battle; spreads volley spikes.")]
        [SerializeField, Min(1)] private int maxStartsPerFrame = 4;
        [Tooltip("Seconds between blasts on one unit while the battle is quiet. A blast lasts ~2 s, so 0.6 s keeps about 3 per unit.")]
        [SerializeField, Min(0f)] private float unitInterval = 0.6f;
        [Tooltip("Blasts smaller than this on screen (diameter, pixels) are skipped.")]
        [SerializeField, Min(0f)] private float minScreenPixels = 6f;

        public int MaxActive => maxActive;
        public int MaxStartsPerFrame => maxStartsPerFrame;
        public float UnitInterval => unitInterval;
        public float MinScreenPixels => minScreenPixels;
    }
}
