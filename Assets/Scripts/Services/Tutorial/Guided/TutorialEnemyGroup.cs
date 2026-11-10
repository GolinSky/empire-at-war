using System;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial.Guided
{
    /// <summary>Enemy units the tutorial spawns once a lesson is finished, between the two stations.</summary>
    [Serializable]
    public sealed class TutorialEnemyGroup
    {
        [Tooltip("Lesson id (HintLesson.Id) that must be finished first. Empty: when the battle starts.")]
        [SerializeField] private string afterLesson;
        [Tooltip("Spawn point on the line between the stations: 0 = enemy station, 1 = player station.")]
        [Range(0f, 1f)] [SerializeField] private float alongFromEnemy = 0.5f;
        [SerializeField] private TutorialFactionUnits[] factions = new TutorialFactionUnits[0];

        public string AfterLesson => afterLesson;
        public float AlongFromEnemy => alongFromEnemy;

        public TutorialFactionUnits GetUnits(FactionType faction)
        {
            foreach (TutorialFactionUnits entry in factions)
                if (entry.Faction == faction) return entry;
            throw new InvalidOperationException($"Tutorial group after '{afterLesson}' has no units for {faction}.");
        }
    }
}
