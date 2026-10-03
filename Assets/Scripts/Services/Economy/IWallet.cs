using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Controllers.Economy
{
    public interface IWallet
    {
        bool TrySpend(UnitRequest unitRequest);

        void Refund(UnitRequest unitRequest);
    }
}