using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.SkirmishGame
{
    public interface ICoreGameCommand: ICommand
    {
        void Play();
        void SpeedUp();
    }
}