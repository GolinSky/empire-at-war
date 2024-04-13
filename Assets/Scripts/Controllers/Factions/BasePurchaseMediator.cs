using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Patterns.ChainOfResponsibility;

namespace EmpireAtWar.Controllers.Factions
{
    public abstract class BasePurchaseMediator: IPurchaseMediator
    {
        protected IChainHandler<UnitRequest> next;


        public virtual void Add(IBuildShipChain buildShipChain)// todo: fix 
        {
            ConstructChains(buildShipChain);
        }

        public abstract void RevertFlow(UnitRequest result);

        protected abstract void ConstructChains(IBuildShipChain buildShipChain);

        public virtual IChainHandler<UnitRequest> SetNext(IChainHandler<UnitRequest> chainHandler)
        {
            next = chainHandler;
            return next;
        }

        public virtual void Handle(UnitRequest request)
        {
            if (next != null)
            {
                next.Handle(request);
            }
        }
    }
}