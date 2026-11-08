using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SuperWeapons;

namespace EmpireAtWar.Services.PlayerCommands
{
    /// <summary>One human's super weapons on the authority.</summary>
    public interface ISuperWeaponCommandHandler
    {
        void Fire(SuperWeaponType type, IEntity target);
    }
}
