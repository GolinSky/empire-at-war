using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Commands.Game
{
    public interface IGameCommand:ICommand
    {
        void StartGame(
            FactionType playerFactionType,
            FactionType enemyFactionType,
            PlanetType planetType,
            BattleVictoryCondition victoryCondition,
            EnemyAiDifficulty enemyDifficulty,
            float startingMoney);
        void ExitGame();
    }
}
