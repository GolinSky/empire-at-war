using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Presenters.Factions
{
    public interface IFactionPresenter
    {
        void ChangeSelection();
        void CloseSelection();
        void BuildUnit(UnitRequest shipType);
        void TryPurchaseUnit(UnitRequest shipType);
        void RevertBuilding(UnitRequest id);
    }
}
