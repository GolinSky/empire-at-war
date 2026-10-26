using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.SuperWeapons;

namespace EmpireAtWar.Services.SuperWeapons
{
    public interface ISuperWeaponFireService
    {
        /// <summary>Raised for every side's firing, so the targeted side can be warned.</summary>
        event Action<SuperWeaponType, IEntity> Fired;

        bool CanTarget(PlayerId owner, IEntity target);

        void Fire(SuperWeaponType type, IEntity target);
    }
}
