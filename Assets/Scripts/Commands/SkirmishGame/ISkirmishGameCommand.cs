using EmpireAtWar.Commands.Game;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands.SkirmishGame
{
    public interface ISkirmishGameCommand: ICommand
    {
        void ChangeTime(GameTimeMode mode); 
    }
}