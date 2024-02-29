using EmpireAtWar.Commands.Game;
using EmpireAtWar.Commands.SkirmishGame;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Views.ShipUi;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.Game
{
    public class SkirmishGameController : Controller<SkirmishGameModel>, ISkirmishGameCommand, IObserver<UserNotifierState>, IInitializable, ILateDisposable
    {
        private readonly IUserStateNotifier userStateNotifier;
        private readonly IGameCommand gameCommand;

        public SkirmishGameController(SkirmishGameModel model, IUserStateNotifier userStateNotifier, IGameCommand gameCommand) : base(model)
        {
            this.userStateNotifier = userStateNotifier;
            this.gameCommand = gameCommand;
        }
        
        public void Initialize()
        {
            userStateNotifier.AddObserver(this);
        }

        public void LateDispose()
        {
            userStateNotifier.RemoveObserver(this);
        }

        public void ChangeTime(GameTimeMode mode)
        {
            switch (mode)
            {
                case GameTimeMode.Common:
                    Time.timeScale = 1.0f;
                    break;
                case GameTimeMode.SpeedUp:
                    Time.timeScale = 4.0f;
                    break;
                case GameTimeMode.Pause:
                    Time.timeScale = 0f;
                    break;
            }

            Model.GameTimeMode = mode;
        }

        public void UpdateState(UserNotifierState notifierState)
        {
            if (notifierState == UserNotifierState.ExitGame)
            {
                ChangeTime(GameTimeMode.Common);
                gameCommand.ExitGame();
                return;
            }
            ChangeTime(notifierState == UserNotifierState.InMenu ? GameTimeMode.Pause : GameTimeMode.Common);
        }
    }
}