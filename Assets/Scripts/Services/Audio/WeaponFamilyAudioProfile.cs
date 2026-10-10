using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [Serializable]
    public sealed class WeaponFamilyAudioProfile
    {
        [SerializeField] private SfxProfile sfx;

        [SerializeField] private WeaponFamily family;

        public WeaponFamily Family => family;
        public SfxProfile Sfx => sfx;
    }
}
