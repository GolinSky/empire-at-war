using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Commands.Game
{
    public interface IGameCommand
    {
        void StartGame(FactionType playerFactionType, FactionType enemyFactionType);
    }
}