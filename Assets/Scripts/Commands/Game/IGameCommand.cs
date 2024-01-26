using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Game
{
    public interface IGameCommand: ICommand
    {
        void ChangeTime(GameTimeMode mode); 
    }
}