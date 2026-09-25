using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    public class SuperWeaponUnitRequest : UnitRequest<SuperWeaponType>
    {
        public SuperWeaponUnitRequest(FactionData factionData, SuperWeaponType key) : base(factionData, key)
        {
        }
    }
}
