using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.SuperWeapons;

namespace EmpireAtWar.Services.SuperWeapons
{
    public interface ISuperWeaponFireService
    {
        bool CanTarget(PlayerId owner, IEntity target);
        void Fire(SuperWeaponType type, IEntity target);
    }
}
