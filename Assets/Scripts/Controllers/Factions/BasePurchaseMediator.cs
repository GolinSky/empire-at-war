using EmpireAtWar.Patterns.ChainOfResponsibility;

namespace EmpireAtWar.Controllers.Factions
{
    public abstract class BasePurchaseMediator: IChainHandler<UnitRequest>
    {
        protected IChainHandler<UnitRequest> next;
        
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