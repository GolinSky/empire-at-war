using EmpireAtWar.Patterns.ChainOfResponsibility;

namespace EmpireAtWar.Controllers.Factions
{
    public abstract class BasePurchaseMediator: IChainHandler<UnitRequest>
    {
        protected IChainHandler<UnitRequest> _next;
        
        public virtual IChainHandler<UnitRequest> SetNext(IChainHandler<UnitRequest> chainHandler)
        {
            _next = chainHandler;
            return _next;
        }

        public virtual void Handle(UnitRequest request)
        {
            if (_next != null)
            {
                _next.Handle(request);
            }
        }
    }
}