using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    /// <summary>Multiplier slider; its range comes from the Slider's min and max in the prefab.</summary>
    public class SettingsSliderRow : MonoBehaviour
    {
        private const float STEP = 0.05f;

        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text valueText;

        public event Action<float> ValueChanged;

        public void Initialize()
        {
            slider.onValueChanged.AddListener(RaiseValueChanged);
        }

        public void Dispose()
        {
            slider.onValueChanged.RemoveListener(RaiseValueChanged);
        }

        public void Render(float value)
        {
            slider.SetValueWithoutNotify(value);
            valueText.text = $"{value:0.00}x";
        }

        private void RaiseValueChanged(float value)
        {
            ValueChanged?.Invoke(Mathf.Round(value / STEP) * STEP);
        }
    }
}
