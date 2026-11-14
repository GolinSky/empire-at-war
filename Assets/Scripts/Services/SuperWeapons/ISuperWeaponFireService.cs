using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.SuperWeapons;

namespace EmpireAtWar.Services.SuperWeapons
{
    public interface ISuperWeaponFireService
    {
        /// <summary>Raised for accepted firing, so the firing and targeted sides can be notified.</summary>
        event Action<PlayerId, SuperWeaponType, IEntity> Fired;

        bool CanTarget(PlayerId owner, IEntity target);

        void Fire(PlayerId issuer, SuperWeaponType type, IEntity target);
    }
}
