using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public class SettingsToggleRow : MonoBehaviour
    {
        private const string ON_LABEL = "On";
        private const string OFF_LABEL = "Off";

        [SerializeField] private Toggle toggle;
        [SerializeField] private TMP_Text stateText;

        public event Action<bool> ValueChanged;

        public void Initialize()
        {
            toggle.onValueChanged.AddListener(RaiseValueChanged);
        }

        public void Dispose()
        {
            toggle.onValueChanged.RemoveListener(RaiseValueChanged);
        }

        public void Render(bool isOn)
        {
            toggle.SetIsOnWithoutNotify(isOn);
            // The state is spelled out so it never relies on color alone.
            stateText.text = isOn ? ON_LABEL : OFF_LABEL;
        }

        private void RaiseValueChanged(bool isOn)
        {
            ValueChanged?.Invoke(isOn);
        }
    }
}
