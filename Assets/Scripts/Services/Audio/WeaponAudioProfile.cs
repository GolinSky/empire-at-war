using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [Serializable]
    public sealed class WeaponAudioProfile
    {
        [SerializeField] private SfxProfile sfx;

        [SerializeField] private WeaponType weaponType;

        public WeaponType WeaponType => weaponType;
        public SfxProfile Sfx => sfx;
    }
}
