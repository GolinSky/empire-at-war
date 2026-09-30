using System;
using UnityEngine;

namespace EmpireAtWar.Services.Settings
{
    [Serializable]
    public sealed class InputSettingsData
    {
        /// <summary>Output of <c>InputActionAsset.SaveBindingOverridesAsJson</c>; empty means authored defaults.</summary>
        [SerializeField] private string bindingOverridesJson = string.Empty;

        public string BindingOverridesJson
        {
            get => bindingOverridesJson;
            set => bindingOverridesJson = value;
        }

        public bool Matches(InputSettingsData other)
        {
            return bindingOverridesJson == other.bindingOverridesJson;
        }
    }
}
