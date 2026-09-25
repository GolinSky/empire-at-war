using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.SuperWeapons
{
    public interface ISuperWeaponFireService
    {
        bool CanTarget(PlayerType owner, IEntity target);
        void Fire(SuperWeaponType type, IEntity target);
    }
}
