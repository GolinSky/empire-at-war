using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    /// <summary>Multiplier or volume slider; its range comes from the Slider's min and max in the prefab.</summary>
    public class SettingsSliderRow : MonoBehaviour
    {
        private const float STEP = 0.05f;

        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text valueText;

        [Tooltip("Shows 0–1 values as a percentage instead of a multiplier.")]
        [SerializeField] private bool showAsPercent;

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
            valueText.text = showAsPercent ? $"{Mathf.RoundToInt(value * 100f)}%" : $"{value:0.00}x";
        }

        private void RaiseValueChanged(float value)
        {
            float step = showAsPercent ? 0.01f : STEP;
            float roundedValue = Mathf.Clamp(Mathf.Round(value / step) * step, slider.minValue, slider.maxValue);
            Render(roundedValue);
            ValueChanged?.Invoke(roundedValue);
        }
    }
}
