using System;

namespace EmpireAtWar.Entities.Game
{
    public sealed class SimulationModel
    {
        public SimulationSetup Setup { get; private set; }
        public SimulationResult Result { get; private set; }
        public float Elapsed { get; private set; }
        public int CountA { get; private set; }
        public int CountB { get; private set; }
        public bool IsSpawning { get; private set; } = true;
        public event Action Changed;
        public SimulationModel(SimulationSetup setup) => Setup = setup;
        public void Reset(SimulationSetup setup)
        {
            Setup = setup; Result = null; Elapsed = 0; CountA = 0; CountB = 0; IsSpawning = true;
            Changed?.Invoke();
        }
        public void CompleteSpawning() => IsSpawning = false;
        public void Advance(float seconds, int countA, int countB, float hullA, float hullB)
        {
            if (Result != null || IsSpawning) return;
            Elapsed += seconds; CountA = countA; CountB = countB;
            BattleOutcome outcome = countA == 0 && countB == 0 ? BattleOutcome.Draw :
                countA == 0 ? BattleOutcome.EnemyVictory : countB == 0 ? BattleOutcome.PlayerVictory :
                Elapsed >= Setup.TimeLimit ? BattleOutcome.Draw : BattleOutcome.None;
            if (outcome != BattleOutcome.None) Result = new SimulationResult(outcome, Elapsed, countA, countB, hullA, hullB);
            Changed?.Invoke();
        }
    }
}
