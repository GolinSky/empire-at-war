namespace EmpireAtWar.Entities.EnemyFaction.Models.Objectives
{
    public readonly struct EnemyZoneAssessment
    {
        public EnemyZoneVerdict Verdict { get; }
        /// <summary>Assessed force over the zone defenders; 1 is an even fight.</summary>
        public float Advantage { get; }
        /// <summary>Multiple of the assessed force the zone needs; 1 or less means the force is enough.</summary>
        public float RequiredScale { get; }
        /// <summary>Zone priority minus the cost of the missing force; higher is better.</summary>
        public float Score { get; }
        public string Reason { get; }

        public EnemyZoneAssessment(EnemyZoneVerdict verdict, float advantage, float requiredScale, float score,
            string reason)
        {
            Verdict = verdict;
            Advantage = advantage;
            RequiredScale = requiredScale;
            Score = score;
            Reason = reason;
        }
    }
}
