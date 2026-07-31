using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Presenters.Factions
{
    public interface IFactionPresenter
    {
        void TryPurchaseUnit(UnitRequest unitRequest);
    }
}
