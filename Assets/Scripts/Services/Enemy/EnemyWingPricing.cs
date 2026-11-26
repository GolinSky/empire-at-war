using System.Collections.Generic;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Whether a faction's capture wings are cheap. Cheap wings (CIS) open with 2 wings on the nearer outer zone and
    /// up to <see cref="CHEAP_OPENING_WING_CAP"/> wings before the first ship; expensive wings (Rebellion) with 1 and
    /// <see cref="OPENING_WING_CAP"/>.
    /// </summary>
    public static class EnemyWingPricing
    {
        /// <summary>A fighter or interceptor wing at or below this price is cheap.</summary>
        public const float CHEAP_WING_PRICE = 300f;
        public const int CHEAP_OPENING_WING_CAP = 6;
        public const int OPENING_WING_CAP = 4;

        public static bool HasCheapFighterWing(IReadOnlyDictionary<SquadronType, FactionData> squadrons,
            UnitCombatProfileCatalog profiles)
        {
            foreach (KeyValuePair<SquadronType, FactionData> option in squadrons)
            {
                ShipClass wingClass = profiles.Get(UnitTypeId.Squadron(option.Key)).ShipClass;
                if (wingClass is ShipClass.Fighter or ShipClass.Interceptor && option.Value.Price <= CHEAP_WING_PRICE)
                    return true;
            }
            return false;
        }
    }
}
