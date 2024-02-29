using EmpireAtWar.Models.Factions;
using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Game
{
    public interface IGameCommand:ICommand
    {
        void StartGame(FactionType playerFactionType, FactionType enemyFactionType);
        void ExitGame();
    }
}