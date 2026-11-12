using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public interface IFactionPresenter
    {
        void TryPurchaseUnit(UnitRequest unitRequest);

        bool IsUnitAvailable(FactionData data);
        /// <summary>Unlocked by the current station level, for the tutorial's new-units mark.</summary>
        bool IsUnlockedAtCurrentLevel(FactionData data);
        /// <summary>A ship with squadron bays, which the tutorial prefers as the new unit to build.</summary>
        bool IsCarrier(UnitRequest request);
        bool IsUnitLimitReached(UnitRequest request);
        int GetUnitSortOrder(UnitRequest request);
    }
}
