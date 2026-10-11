namespace EmpireAtWar.Services.Radar
{
    internal sealed class RadarRegistration
    {
        public IRadarScanner Scanner { get; }
        public int IntervalSteps { get; }
        public int StepsUntilScan { get; set; }
        public bool IsRegistered { get; set; } = true;

        public RadarRegistration(IRadarScanner scanner, int intervalSteps, int stepsUntilScan)
        {
            Scanner = scanner;
            IntervalSteps = intervalSteps;
            StepsUntilScan = stepsUntilScan;
        }
    }
}
