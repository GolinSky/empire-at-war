using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SuperWeapons;
using UnityEngine;

namespace EmpireAtWar.Services.SuperWeapons
{
    /// <summary>One firing in flight: shots still to fire and seconds until each fired shot lands.</summary>
    public sealed class SuperWeaponSalvo
    {
        public SuperWeaponProfile Profile { get; }
        public IEntity Target { get; }
        public Transform Origin { get; }
        public List<float> ImpactTimes { get; } = new List<float>();
        public int ShotsFired { get; set; }
        public float NextShotTime { get; set; }
        public bool IsComplete => ShotsFired >= Profile.Weapon.ShotsPerSalvo && ImpactTimes.Count == 0;

        public SuperWeaponSalvo(SuperWeaponProfile profile, IEntity target, Transform origin)
        {
            Profile = profile;
            Target = target;
            Origin = origin;
            NextShotTime = profile.FiringDelay;
        }
    }
}
