using System;
using UnityEngine;

namespace EmpireAtWar.Services.Settings
{
    [Serializable]
    public sealed class GameplaySettingsData
    {
        [SerializeField] private bool autoPowerAbilities = true;

        public bool AutoPowerAbilities
        {
            get => autoPowerAbilities;
            set => autoPowerAbilities = value;
        }

        public bool Matches(GameplaySettingsData other)
        {
            return autoPowerAbilities == other.autoPowerAbilities;
        }
    }
}
