using EmpireAtWar.Mvc;

namespace EmpireAtWar.Commands.SkirmishGame
{
    public interface ICoreGameCommand: ICommand
    {
        void Play();
        void SpeedUp();
    }
}