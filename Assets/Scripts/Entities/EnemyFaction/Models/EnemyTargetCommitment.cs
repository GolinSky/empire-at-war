using System;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public sealed class EnemyTargetCommitment
    {
        public const float MIN_TARGET_SECONDS = 10f;
        public const float SWITCH_MARGIN = 0.2f;
        private string _target;
        private float _selectedAt;
        public bool Changed { get; private set; }
        public string LastCause { get; private set; }
        public string LastFromId { get; private set; }
        public string LastToId { get; private set; }
        public float LastFromScore { get; private set; }
        public float LastToScore { get; private set; }
        public float LastHeldSeconds { get; private set; }
        public float HeldSeconds(float now) => _target == null ? 0f : Math.Max(0f, now - _selectedAt);

        // Scores are utilities: larger is better, including negative costs.
        public string Select(string candidateId, float candidateScore, string currentId, float currentScore,
            bool currentValid, float now, string invalidReason = "unavailable")
        {
            Changed = false;
            float held = HeldSeconds(now);
            bool keep = currentValid && currentId != null && (candidateId == currentId ||
                candidateId == null || held < MIN_TARGET_SECONDS ||
                candidateScore - currentScore < Math.Max(0.001f, Math.Abs(currentScore) * SWITCH_MARGIN));
            string selected = keep ? currentId : candidateId;
            LastCause = currentValid ? "better" : "invalid:" + invalidReason;
            if (selected == _target) return selected;
            Changed = true;
            LastFromId = _target;
            LastToId = selected;
            LastFromScore = currentScore;
            LastToScore = keep ? currentScore : candidateScore;
            LastHeldSeconds = held;
            _target = selected;
            _selectedAt = now;
            return selected;
        }
    }
}
