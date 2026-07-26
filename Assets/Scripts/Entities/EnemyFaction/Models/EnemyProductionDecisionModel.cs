using System;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public enum EnemyProductionCategory
    {
        None = 0,
        Ship = 1,
        Mining = 2,
        Defense = 3,
        Level = 4
    }

    public readonly struct EnemyProductionSnapshot
    {
        public EnemyProductionSnapshot(
            EnemyStrategicState strategicState,
            EnemyAiDifficulty difficulty,
            bool canBuildShip,
            bool canBuildMining,
            bool canBuildDefense,
            bool canLevelUp)
        {
            StrategicState = strategicState;
            Difficulty = difficulty;
            CanBuildShip = canBuildShip;
            CanBuildMining = canBuildMining;
            CanBuildDefense = canBuildDefense;
            CanLevelUp = canLevelUp;
        }

        public EnemyStrategicState StrategicState { get; }
        public EnemyAiDifficulty Difficulty { get; }
        public bool CanBuildShip { get; }
        public bool CanBuildMining { get; }
        public bool CanBuildDefense { get; }
        public bool CanLevelUp { get; }
    }

    public sealed class EnemyProductionDecisionModel
    {
        public EnemyProductionCategory Evaluate(EnemyProductionSnapshot snapshot)
        {
            if (snapshot.StrategicState == EnemyStrategicState.DefendBase &&
                snapshot.Difficulty >= EnemyAiDifficulty.Hard &&
                snapshot.CanBuildDefense)
            {
                return EnemyProductionCategory.Defense;
            }

            if (snapshot.StrategicState == EnemyStrategicState.Hold)
            {
                if (snapshot.Difficulty >= EnemyAiDifficulty.Hard && snapshot.CanLevelUp)
                {
                    return EnemyProductionCategory.Level;
                }

                if (snapshot.CanBuildMining)
                {
                    return EnemyProductionCategory.Mining;
                }
            }

            if (snapshot.CanBuildShip)
            {
                return EnemyProductionCategory.Ship;
            }

            if (snapshot.CanBuildDefense)
            {
                return EnemyProductionCategory.Defense;
            }

            if (snapshot.CanBuildMining)
            {
                return EnemyProductionCategory.Mining;
            }

            return snapshot.CanLevelUp
                ? EnemyProductionCategory.Level
                : EnemyProductionCategory.None;
        }
    }
}
