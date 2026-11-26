using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.UnitOrders
{
    /// <summary>Cap by target <see cref="ShipClass"/> from <see cref="UnitOrderSettings"/>; a hero target gets extra slots.</summary>
    public sealed class FocusFireCaps : IFocusFireCaps
    {
        public const int UNLIMITED = 0;

        private readonly UnitOrderSettings _settings;
        private readonly FactionCatalog _factions;

        public FocusFireCaps(UnitOrderSettings settings, FactionCatalog factions)
        {
            _settings = settings;
            _factions = factions;
        }

        public int GetCap(IEntity target, bool isSquadron)
        {
            int cap = _settings.GetFocusFireCap(target.HealthModel.ShipClass, isSquadron);
            if (cap == UNLIMITED || !IsHero(target)) return cap;
            return cap + _settings.HeroExtraSlots;
        }

        private bool IsHero(IEntity target)
        {
            if (!target.TryGetFacade(out IUnitTypeFacade unit)) return false;
            UnitTypeId type = unit.UnitTypeId;
            if (type.IsShip) return _factions.GetShipFactionData(type.ShipType).IsHero;
            return type.IsSquadron && _factions.GetSquadronFactionData(type.SquadronType).IsHero;
        }
    }
}
