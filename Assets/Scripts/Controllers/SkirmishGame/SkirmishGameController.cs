using EmpireAtWar.Commands.Game;
using EmpireAtWar.Commands.SkirmishGame;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using LightWeightFramework.Controller;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Controllers.Game
{
    public interface IPurchaseChain:IChainHandler<ShipType>
    {
        
    }
    public class SkirmishGameController : Controller<SkirmishGameModel>, ISkirmishGameCommand, IObserver<UserNotifierState>, IInitializable, ILateDisposable, ITickable, IPurchaseChain
    {
        private const float DefaultIncome = 1f;
        private const float SpeedUpTimeScale = 4f;
        private const float DefaultTimeScale = 1f;
        private const float PauseTimeScale = 0f;
        private readonly IUserStateNotifier userStateNotifier;
        private readonly IGameCommand gameCommand;
        private readonly FactionsModel factionsModel;
        private readonly ITimer incomeTimer;
        private GameTimeMode gameTimeMode;

        [Inject(Id = PlayerType.Player)] 
        private FactionType PlayerFactionType { get; }
        public SkirmishGameController(SkirmishGameModel model, IUserStateNotifier userStateNotifier, IGameCommand gameCommand, FactionsModel factionsModel) : base(model)
        {
            this.userStateNotifier = userStateNotifier;
            this.gameCommand = gameCommand;
            this.factionsModel = factionsModel;
            gameTimeMode = GameTimeMode.Common;
            ChangeTime(gameTimeMode);
            incomeTimer = TimerFactory.ConstructTimer(model.IncomeDelay);
            Model.Money = Model.StartMoneyAmount;
        }
        
        public void Initialize()
        {
            userStateNotifier.AddObserver(this);
        }

        public void LateDispose()
        {
            userStateNotifier.RemoveObserver(this);
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

        private bool TryBuyUnit(float price)
        {
            if (Model.Money > price)
            {
                Model.Money -= price;
                return true;
            }

            return false;
        }

        public void Tick()
        {
            if (incomeTimer.IsComplete)
            {
                incomeTimer.StartTimer();
                Model.Money += DefaultIncome;
            }
        }

        private IChainHandler<ShipType> nextChain;

        public IChainHandler<ShipType> SetNext(IChainHandler<ShipType> chainHandler)
        {
            nextChain = chainHandler;
            return nextChain;
        }

        public void Handle(ShipType shipType)
        {
            FactionData factionData = factionsModel.GetFactionData(PlayerFactionType)[shipType];
            if (TryBuyUnit(factionData.Price))
            {
                if (nextChain != null)
                {
                    nextChain.Handle(shipType);
                }
            }
        }
    }
}