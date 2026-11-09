using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    [CreateAssetMenu(menuName = "EmpireAtWar/Tutorial/Hint Track", fileName = nameof(HintTrack))]
    public sealed class HintTrack : ScriptableObject
    {
        [Tooltip("Shown in this order from match start; the next waits until the previous is done or dismissed.")]
        [SerializeField] private HintDefinition[] basics = new HintDefinition[0];
        [Tooltip("One-shot hints fired by their trigger.")]
        [SerializeField] private HintDefinition[] events = new HintDefinition[0];

        [Header("Pacing")]
        [Min(0f)] [SerializeField] private float minimumGapSeconds = 3f;
        [Tooltip("Hints without Show During Combat wait this long after a local unit last took damage.")]
        [Min(0f)] [SerializeField] private float combatQuietSeconds = 5f;
        [Tooltip("Longest a waiting hint is held back by ongoing combat; long fights would otherwise stall the tutorial.")]
        [Min(0f)] [SerializeField] private float maxCombatHoldSeconds = 20f;

        public IReadOnlyList<HintDefinition> Basics => basics;
        public IReadOnlyList<HintDefinition> Events => events;
        public float MinimumGapSeconds => minimumGapSeconds;
        public float CombatQuietSeconds => combatQuietSeconds;
        public float MaxCombatHoldSeconds => maxCombatHoldSeconds;
    }
}
