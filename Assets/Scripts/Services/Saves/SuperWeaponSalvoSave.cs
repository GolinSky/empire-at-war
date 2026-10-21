using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Services.SuperWeapons;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    [Serializable]
    public sealed class SuperWeaponSalvoSave
    {
        [SerializeField] private SuperWeaponType type;
        [SerializeField] private long targetId;
        [SerializeField] private Vector3 originPosition;
        [SerializeField] private Quaternion originRotation;
        [SerializeField] private int shotsFired;
        [SerializeField] private float nextShotTime;
        [SerializeField] private float[] impactTimes;

        public static SuperWeaponSalvoSave From(SuperWeaponSalvo salvo) => new SuperWeaponSalvoSave
        {
            type = salvo.Type,
            targetId = salvo.Target.Id,
            originPosition = salvo.Origin.position,
            originRotation = salvo.Origin.rotation,
            shotsFired = salvo.ShotsFired,
            nextShotTime = salvo.NextShotTime,
            impactTimes = salvo.ImpactTimes.ToArray()
        };

        public SuperWeaponSalvo Restore(IEntityLocator entities, SuperWeaponData data)
        {
            IEntity target = entities.GetEntity(targetId);
            GameObject origin = new GameObject($"{type}Origin");
            origin.transform.SetPositionAndRotation(originPosition, originRotation);
            SuperWeaponSalvo salvo = new SuperWeaponSalvo(type, target, data.GetProfile(type), origin.transform)
            {
                ShotsFired = shotsFired,
                NextShotTime = nextShotTime
            };
            salvo.ImpactTimes.AddRange(impactTimes);
            return salvo;
        }
    }
}
