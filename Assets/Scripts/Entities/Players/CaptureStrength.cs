namespace EmpireAtWar.Models.Players
{
    /// <summary>
    /// Who is winning a capture circle this frame. Strength is summed per team; the strongest team
    /// captures for its strongest player, at a speed set by its lead over the runner-up team.
    /// </summary>
    public readonly struct CaptureStrength
    {
        private const float TIE_EPSILON = 0.001f;

        /// <summary>Strongest player of the strongest team; <see cref="PlayerId.None"/> when the circle is empty.</summary>
        public PlayerId LeadingPlayer { get; }
        /// <summary>Leading team strength minus the runner-up team strength (ship = 1).</summary>
        public float Advantage { get; }
        public int PresentTeamCount { get; }
        public bool HasUnits => PresentTeamCount > 0;
        public bool IsTied => Advantage < TIE_EPSILON;
        /// <summary>Two or more teams hold the circle with equal strength.</summary>
        public bool IsContested => PresentTeamCount >= 2 && IsTied;

        public CaptureStrength(PlayerId leadingPlayer, float advantage, int presentTeamCount)
        {
            LeadingPlayer = leadingPlayer;
            Advantage = advantage;
            PresentTeamCount = presentTeamCount;
        }
    }
}
