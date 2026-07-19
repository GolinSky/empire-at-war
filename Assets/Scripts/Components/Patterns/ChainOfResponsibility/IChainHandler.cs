namespace EmpireAtWar.Patterns.ChainOfResponsibility
{
    public interface IChainHandler<TRequest>
    {
        IChainHandler<TRequest> SetNext(IChainHandler<TRequest> chainHandler);
        void Handle(TRequest request);
    }
}