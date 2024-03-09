using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using LightWeightFramework.Controller;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Controllers.Economy
{
    public class EconomyController : Controller<EconomyModel>, IPurchaseChain, ITickable
    {
        private const float DefaultIncome = 1f;

        private readonly ITimer incomeTimer;
        private IChainHandler<UnitRequest> nextChain;
        
        public EconomyController(EconomyModel model) : base(model)
        {
            incomeTimer = TimerFactory.ConstructTimer(model.IncomeDelay);
            Model.Money = Model.StartMoneyAmount;
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
        
        public IChainHandler<UnitRequest> SetNext(IChainHandler<UnitRequest> chainHandler)
        {
            nextChain = chainHandler;
            return nextChain;
        }

        public void Handle(UnitRequest unitRequest)
        {
            if (TryBuyUnit(unitRequest.FactionData.Price))
            {
                if (nextChain != null)
                {
                    nextChain.Handle(unitRequest);
                }
            }
        }

        public void Revert(UnitRequest unitRequest)
        {
            Model.Money += unitRequest.FactionData.Price;
        }
    }
}