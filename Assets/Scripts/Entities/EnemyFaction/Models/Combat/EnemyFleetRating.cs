namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>
    /// Reach-based fleet rating (W63, decision D1): a group compares what can be at its area within
    /// <see cref="REACH_WINDOW_SECONDS"/> on both sides, not itself against the whole hostile team. Each side adds the
    /// credits it can turn into ships in that window, rated at the strength per credit of a reference force.
    /// </summary>
    public static class EnemyFleetRating
    {
        /// <summary>
        /// About how long a fleet fight at a zone takes to decide: help that arrives later meets the result, not the
        /// fight. It also covers a non-capital build (8–40 s) plus the drop.
        /// </summary>
        public const float REACH_WINDOW_SECONDS = 60f;

        /// <summary>A unit inside the area always counts; one outside needs a speed that covers the gap in time.</summary>
        public static bool CanReach(float distance, float areaRadius, float speed, float seconds) =>
            distance <= areaRadius || speed > 0f && distance - areaRadius <= speed * seconds;

        /// <summary>
        /// <see cref="CombatMatchup.Advantage"/> of <paramref name="own"/> plus <paramref name="ownCredits"/> over
        /// <paramref name="hostile"/> plus <paramref name="hostileCredits"/>. Credits buy more of
        /// <paramref name="reference"/> (worth <paramref name="referenceValue"/>) on both sides: the hostile strength per
        /// credit is not known, so a credit is worth the same to each side. Without a priced reference credits are left out.
        /// </summary>
        /// <param name="ownTotal">Scratch force; holds the own side afterwards.</param>
        /// <param name="hostileTotal">Scratch force; holds the hostile side afterwards.</param>
        public static float Rate(ForceComposition own, ForceComposition hostile, ForceComposition reference,
            float referenceValue, float ownCredits, float hostileCredits, ForceComposition ownTotal,
            ForceComposition hostileTotal)
        {
            ownTotal.CopyFrom(own);
            hostileTotal.CopyFrom(hostile);
            if (referenceValue > 0f && !reference.IsEmpty)
            {
                if (ownCredits > 0f) ownTotal.AddShare(reference, ownCredits / referenceValue);
                if (hostileCredits > 0f) hostileTotal.AddShare(reference, hostileCredits / referenceValue);
            }
            return CombatMatchup.Advantage(ownTotal, hostileTotal);
        }
    }
}
