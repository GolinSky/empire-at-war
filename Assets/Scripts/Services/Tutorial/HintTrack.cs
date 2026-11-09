using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    [CreateAssetMenu(menuName = "EmpireAtWar/Tutorial/Hint Track", fileName = nameof(HintTrack))]
    public sealed class HintTrack : ScriptableObject
    {
        [Tooltip("Onboarding in order. The first lesson whose trigger happened runs next; a waiting one never blocks later ones.")]
        [SerializeField] private HintLesson[] onboarding = new HintLesson[0];
        [Tooltip("One-shot lessons fired by their trigger. They wait while an onboarding lesson is ready.")]
        [SerializeField] private HintLesson[] events = new HintLesson[0];

        [Header("Pacing")]
        [Tooltip("Pause between lessons.")]
        [Min(0f)] [SerializeField] private float minimumGapSeconds = 3f;
        [Tooltip("Pause between steps of one lesson; long enough for the done mark.")]
        [Min(0f)] [SerializeField] private float stepGapSeconds = 0.6f;
        [Tooltip("Steps without Show During Combat wait this long after a local unit last took damage.")]
        [Min(0f)] [SerializeField] private float combatQuietSeconds = 5f;
        [Tooltip("Longest a waiting step is held back by ongoing combat; long fights would otherwise stall the tutorial.")]
        [Min(0f)] [SerializeField] private float maxCombatHoldSeconds = 20f;

        public IReadOnlyList<HintLesson> Onboarding => onboarding;
        public IReadOnlyList<HintLesson> Events => events;
        public float MinimumGapSeconds => minimumGapSeconds;
        public float StepGapSeconds => stepGapSeconds;
        public float CombatQuietSeconds => combatQuietSeconds;
        public float MaxCombatHoldSeconds => maxCombatHoldSeconds;
    }
}
