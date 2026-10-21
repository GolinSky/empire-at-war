using System;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.SuperWeapons;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    [Serializable]
    public sealed class SuperWeaponStunSave
    {
        [SerializeField] private long targetId;
        [SerializeField] private CombatStatModifier modifier;
        [SerializeField] private float timeLeft;

        public static SuperWeaponStunSave From(SuperWeaponStun stun) => new SuperWeaponStunSave
        {
            targetId = stun.Target.Id,
            modifier = stun.Modifier,
            timeLeft = stun.TimeLeft
        };

        public SuperWeaponStun Restore(IEntityLocator entities)
        {
            IEntity target = entities.GetEntity(targetId);
            return new SuperWeaponStun(target, target.GetFacade<ICombatModifiersFacade>().Modifiers, modifier, timeLeft);
        }
    }
}
