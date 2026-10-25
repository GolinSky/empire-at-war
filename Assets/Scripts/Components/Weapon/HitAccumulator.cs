namespace EmpireAtWar.Components.Weapon
{
    /// <summary>
    /// Spreads one weapon's hits evenly instead of rolling every shot on its own: each shot adds its hit chance
    /// to a credit and hits once the credit reaches 1. A random starting credit and a small per-shot jitter keep
    /// the pattern unpredictable; over many shots the hit ratio still equals the hit chance.
    /// </summary>
    public sealed class HitAccumulator
    {
        private const float JITTER = 0.15f;

        private float _credit;

        /// <param name="startRoll">Uniform value in [0, 1) that sets where the first hit falls.</param>
        public HitAccumulator(float startRoll)
        {
            _credit = startRoll;
        }

        /// <param name="jitterRoll">Uniform value in [0, 1); shifts this shot's credit by up to ±<see cref="JITTER"/>.</param>
        public bool Roll(float hitChance, float jitterRoll)
        {
            if (hitChance >= 1f) return true;
            if (hitChance <= 0f) return false;

            _credit += hitChance + (jitterRoll * 2f - 1f) * JITTER;
            if (_credit < 1f) return false;
            _credit -= 1f;
            return true;
        }
    }
}
