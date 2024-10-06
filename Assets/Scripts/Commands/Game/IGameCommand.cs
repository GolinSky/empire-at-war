using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Planet;
using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Game
{
    public interface IGameCommand:ICommand
    {
        void StartGame(FactionType playerFactionType, FactionType enemyFactionType, PlanetType planetType);
        void ExitGame();
    }
}