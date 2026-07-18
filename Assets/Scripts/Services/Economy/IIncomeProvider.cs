namespace EmpireAtWar.Controllers.Economy
{
    public interface IEconomyProvider
    {
        void AddProvider(IIncomeProvider incomeProvider);
        void RemoveProvider(IIncomeProvider incomeProvider);
        void RecalculateIncome(IIncomeProvider incomeProvider);
    }

    public interface IIncomeProvider
    {
        float Income { get; }
    }
}
