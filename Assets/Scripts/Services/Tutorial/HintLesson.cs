using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>One lesson: small steps shown one after another. Steps already done are skipped.</summary>
    [CreateAssetMenu(menuName = "EmpireAtWar/Tutorial/Hint Lesson", fileName = "HintLesson")]
    public sealed class HintLesson : ScriptableObject
    {
        [Tooltip("Stable id stored in hint progress. Never rename after release.")]
        [SerializeField] private string id;
        [SerializeField] private HintDefinition[] steps = new HintDefinition[0];
        [Tooltip("Starts the lesson the first time this happens. None: as soon as pacing allows.")]
        [SerializeField] private HintConditionId trigger;
        [Tooltip("Event lessons: higher starts first when several are ready.")]
        [SerializeField] private int priority;
        [Tooltip("Event lesson that may start while an onboarding lesson is ready (enemy contact, Survival).")]
        [SerializeField] private bool interruptsOnboarding;

        public string Id => id;
        public IReadOnlyList<HintDefinition> Steps => steps;
        public HintConditionId Trigger => trigger;
        public int Priority => priority;
        public bool InterruptsOnboarding => interruptsOnboarding;
    }
}
