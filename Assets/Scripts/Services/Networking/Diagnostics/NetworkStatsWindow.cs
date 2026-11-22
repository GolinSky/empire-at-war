namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>Shared numbers of one collection window, handed to the role-specific stats.</summary>
    public readonly struct NetworkStatsWindow
    {
        public NetworkStatsWindow(float seconds, float fps, float shotsPerSecond)
        {
            Seconds = seconds;
            Fps = fps;
            ShotsPerSecond = shotsPerSecond;
        }

        public float Seconds { get; }
        public float Fps { get; }
        public float ShotsPerSecond { get; }
    }
}
