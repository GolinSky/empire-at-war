using EmpireAtWar.Services.Saves;
using System.Collections.Generic;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Commands.Game
{
    public interface IGameCommand
    {
        void StartBattle(BattleDefinition definition);
        void LoadBattle(BattleSave save);

        void ExitGame();
    }
}
