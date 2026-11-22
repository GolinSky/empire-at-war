namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>
    /// Online client: horizontal distance between each squadron's host pose and its locally simulated fighter
    /// centroid. Drift that stays above the loiter radius means the fighters fly somewhere other than the host's
    /// squadron (symptom D1).
    /// </summary>
    public sealed class SquadronDriftStats
    {
        private double _sum;

        public int Count { get; private set; }
        public float Max { get; private set; }
        public int BeyondLoiter { get; private set; }
        public double Mean => Count > 0 ? _sum / Count : double.NaN;

        public void Add(float distance, float loiterRadius)
        {
            Count++;
            _sum += distance;
            if (distance > Max) Max = distance;
            if (distance > loiterRadius) BeyondLoiter++;
        }

        public void Reset()
        {
            _sum = 0;
            Count = 0;
            Max = 0f;
            BeyondLoiter = 0;
        }
    }
}
