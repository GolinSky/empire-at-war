using EmpireAtWar.Commands.Game;
using EmpireAtWar.Commands.SkirmishGame;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishGame;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.Game
{

    public class SkirmishGameController : Controller<SkirmishGameModel>, ISkirmishGameCommand, IObserver<UserNotifierState>, IInitializable, ILateDisposable
    {
        private const float SpeedUpTimeScale = 4f;
        private const float DefaultTimeScale = 1f;
        private const float PauseTimeScale = 0f;
        private readonly LazyInject<IUserStateNotifier> userStateNotifier;
        private readonly IGameCommand gameCommand;
        private readonly FactionsModel factionsModel;
        private GameTimeMode gameTimeMode;
        
        public SkirmishGameController(SkirmishGameModel model, LazyInject<IUserStateNotifier> userStateNotifier, IGameCommand gameCommand) : base(model)
        {
            this.userStateNotifier = userStateNotifier;
            this.gameCommand = gameCommand;
            gameTimeMode = GameTimeMode.Common;
            ChangeTime(gameTimeMode);
        }
        
        public void Initialize()
        {
            userStateNotifier.Value.AddObserver(this);
        }

        public void LateDispose()
        {
            userStateNotifier.Value.RemoveObserver(this);
        }

        public void Play()
        {
            switch (gameTimeMode)
            {
                case GameTimeMode.Common:
                    gameTimeMode = GameTimeMode.Pause;
                    break;
                case GameTimeMode.SpeedUp:
                    gameTimeMode = GameTimeMode.Pause;
                    break;
                case GameTimeMode.Pause:
                    gameTimeMode = GameTimeMode.Common;
                    break;
            }

            ChangeTime(gameTimeMode);
        }

        public void SpeedUp()
        {
            switch (gameTimeMode)
            {
                case GameTimeMode.Common:
                    gameTimeMode = GameTimeMode.SpeedUp;
                    break;
                case GameTimeMode.SpeedUp:
                    gameTimeMode = GameTimeMode.Common;
                    break;
                case GameTimeMode.Pause:
                    gameTimeMode = GameTimeMode.SpeedUp;
                    break;
            }
            ChangeTime(gameTimeMode);
        }
        
        private void ChangeTime(GameTimeMode mode)
        {
            switch (mode)
            {
                case GameTimeMode.Common:
                    Time.timeScale = DefaultTimeScale;
                    break;
                case GameTimeMode.SpeedUp:
                    Time.timeScale = SpeedUpTimeScale;
                    break;
                case GameTimeMode.Pause:
                    Time.timeScale = PauseTimeScale;
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