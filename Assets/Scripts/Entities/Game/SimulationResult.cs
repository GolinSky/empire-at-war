namespace EmpireAtWar.Entities.Game
{
    public sealed class SimulationResult
    {
        public BattleOutcome Outcome { get; }
        public float Elapsed { get; }
        public int SurvivorsA { get; }
        public int SurvivorsB { get; }
        public float HullA { get; }
        public float HullB { get; }
        public SimulationResult(BattleOutcome outcome, float elapsed, int survivorsA, int survivorsB, float hullA, float hullB)
        {
            Outcome = outcome; Elapsed = elapsed; SurvivorsA = survivorsA; SurvivorsB = survivorsB;
            HullA = hullA; HullB = hullB;
        }
    }
}
