using EmpireAtWar.Commands.Game;
using EmpireAtWar.Models.Game;
using LightWeightFramework.Controller;
using UnityEngine;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Controllers.Game
{
    public class GameController:Controller<GameModel>, IGameCommand
    {
        public GameController(GameModel model) : base(model)
        {
        }

        public void ChangeTime(GameTimeMode mode)
        {
            switch (mode)
            {
                case GameTimeMode.Common:
                    Time.timeScale = 1.0f;
                    break;
                case GameTimeMode.SpeedUp:
                    Time.timeScale = 2.0f;
                    break;
                case GameTimeMode.Pause:
                    Time.timeScale = 0f;
                    break;
            }

            Model.GameTimeMode = mode;
        }
        
        public bool TryGetCommand<TCommand>(out TCommand command) where TCommand : ICommand
        {
            command = default;
            return false;
        }
    }
}