using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public interface IFactionPresenter
    {
        /// <summary>Submits the purchase; false when it was rejected (the presenter already played the refusal).</summary>
        bool TryPurchaseUnit(UnitRequest unitRequest);

        bool IsUnitAvailable(FactionData data);
        /// <summary>Unlocked by the current station level, for the tutorial's new-units mark.</summary>
        bool IsUnlockedAtCurrentLevel(FactionData data);
        /// <summary>A ship with squadron bays, which the tutorial prefers as the new unit to build.</summary>
        bool IsCarrier(UnitRequest request);
        bool IsScout(UnitRequest request);
        bool IsUnitLimitReached(UnitRequest request);
        ShipClass GetUnitClass(UnitRequest request);
        int GetUnitSortOrder(UnitRequest request);
    }
}
