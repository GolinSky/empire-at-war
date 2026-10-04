using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Presenters.Factions
{
    public interface IFactionPresenter
    {
        void TryPurchaseUnit(UnitRequest unitRequest);

        bool IsUnitAvailable(EmpireAtWar.Models.Factions.FactionData data);
        bool IsUnitLimitReached(UnitRequest request);
    }
}
