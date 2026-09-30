using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [Serializable]
    public sealed class WeaponAudioProfile
    {
        [SerializeField] private WeaponType weaponType;
        [SerializeField] private SfxProfile sfx;

        public WeaponType WeaponType => weaponType;
        public SfxProfile Sfx => sfx;
    }
}
