using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Commands.Game
{
    public interface IGameCommand:ICommand
    {
        void StartGame(FactionType playerFactionType, FactionType enemyFactionType, PlanetType planetType);
        void ExitGame();
    }
}