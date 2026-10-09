using System.Collections.Generic;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Commands.Game
{
    public interface IGameCommand
    {
        void StartGame(
            IReadOnlyList<PlayerSlot> players,
            PlanetType planetType,
            MapSize mapSize,
            BattleVictoryCondition victoryCondition,
            float startingMoney);

        void ExitGame();
    }
}
