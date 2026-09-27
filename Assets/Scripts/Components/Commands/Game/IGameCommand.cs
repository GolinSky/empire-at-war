using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Commands.Game
{
    public interface IGameCommand
    {
        void StartGame(
            FactionType playerFactionType,
            FactionType enemyFactionType,
            PlanetType planetType,
            MapSize mapSize,
            BattleVictoryCondition victoryCondition,
            EnemyAiDifficulty enemyDifficulty,
            float startingMoney);
        void ExitGame();
    }
}
