using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial.Guided
{
    /// <summary>Content of the guided tutorial battle: its lessons, scripted enemy groups and money top-up.</summary>
    [CreateAssetMenu(menuName = "EmpireAtWar/Tutorial/Guided Tutorial Script", fileName = nameof(TutorialScript))]
    public sealed class TutorialScript : ScriptableObject
    {
        [Tooltip("Chapters in order (onboarding) and situation lessons (events).")]
        [SerializeField] private HintTrack track;
        [Tooltip("The player's money is topped up to this amount whenever it drops below, per station level (index 0 = level 1). The last value covers higher levels.")]
        [SerializeField] private float[] moneyFloorPerLevel = { 2000f };
        [Tooltip("Station upgrades, ships and facilities build this many times faster, so lessons do not wait long.")]
        [Min(1f)] [SerializeField] private float buildSpeed = 3f;
        [Tooltip("Units of one group spawn in a circle of this radius.")]
        [Min(0f)] [SerializeField] private float spawnSpread = 60f;
        [SerializeField] private TutorialEnemyGroup[] enemyGroups = new TutorialEnemyGroup[0];

        public HintTrack Track => track;
        public float BuildSpeed => buildSpeed;
        public float SpawnSpread => spawnSpread;
        public IReadOnlyList<TutorialEnemyGroup> EnemyGroups => enemyGroups;

        public float GetMoneyFloor(int stationLevel) =>
            moneyFloorPerLevel[Mathf.Clamp(stationLevel - 1, 0, moneyFloorPerLevel.Length - 1)];
    }
}
