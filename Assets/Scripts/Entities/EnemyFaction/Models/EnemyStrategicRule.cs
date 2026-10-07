using System;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>
    /// One option the AI can pick: its utility is <see cref="Weight"/> times a 0..1 consideration of the snapshot.
    /// The weight orders rules that are fully satisfied; considerations let a weak case lose to a strong one.
    /// </summary>
    public sealed class EnemyStrategicRule
    {
        private readonly Func<EnemyStrategicSnapshot, EnemyAiDifficultyProfile, float> _consideration;

        public EnemyStrategicState State { get; }
        public float Weight { get; }
        public string Reason { get; }

        public EnemyStrategicRule(
            EnemyStrategicState state,
            float weight,
            string reason,
            Func<EnemyStrategicSnapshot, EnemyAiDifficultyProfile, float> consideration)
        {
            State = state;
            Weight = weight;
            Reason = reason;
            _consideration = consideration;
        }

        public float Score(EnemyStrategicSnapshot snapshot, EnemyAiDifficultyProfile profile) =>
            Weight * _consideration(snapshot, profile);
    }
}
