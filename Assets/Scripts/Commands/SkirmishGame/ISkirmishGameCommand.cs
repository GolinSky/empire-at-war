using EmpireAtWar.Commands.Game;
using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.SkirmishGame
{
    public interface ISkirmishGameCommand: ICommand
    {
        void ChangeTime(GameTimeMode mode); 
    }
}