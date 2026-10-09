using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceStatSlider
    {
        public static float Maximum(BalanceField field, BalanceDraft draft)
        {
            float saved = float.Parse(field.Read(), CultureInfo.InvariantCulture);
            float value = float.Parse(field.DraftValue(draft), CultureInfo.InvariantCulture);
            double maximum = Math.Max(100, saved * 2d);
            if (float.IsFinite(value)) maximum = Math.Max(maximum, value);
            return (float)Math.Min(maximum, Math.Min(field.Maximum, float.MaxValue));
        }

        public static VisualElement Create(BalanceField field, BalanceDraft draft, Action<string> edit, float maximum)
        {
            float initial = float.Parse(field.DraftValue(draft), CultureInfo.InvariantCulture);
            VisualElement control = new VisualElement { userData = field.Key };
            control.AddToClassList("balance-stat-editor");
            control.AddToClassList(field.Stat.Contains("Shield") ? "balance-shield-editor" : "balance-hull-editor");
            control.EnableInClassList("balance-changed", field.DraftValue(draft) != field.Read());
            VisualElement row = new VisualElement(); row.AddToClassList("balance-stat-inputs"); control.Add(row);
            Slider slider = new Slider((float)field.Minimum, maximum) { name = "stat-slider" };
            slider.AddToClassList("balance-stat-slider"); row.Add(slider);
            FloatField input = new FloatField { name = "stat-value", isDelayed = true };
            input.AddToClassList("balance-stat-input"); row.Add(input);
            input.tooltip = field.Label + " · saved value: " + field.Read();
            VisualElement fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("balance-stat-fill"); slider.Q("unity-tracker").Add(fill);
            HelpBox error = new HelpBox("", HelpBoxMessageType.Error); control.Add(error);
            bool dragging = false;

            void Display(float value)
            {
                input.SetValueWithoutNotify(value);
                string message = field.Validate(value.ToString("R", CultureInfo.InvariantCulture));
                error.text = message; error.style.display = message.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
                slider.SetEnabled(message.Length == 0);
                if (message.Length != 0) return;
                slider.highValue = Math.Max(slider.highValue, value);
                slider.SetValueWithoutNotify(value);
                fill.style.width = Length.Percent(Mathf.InverseLerp(slider.lowValue, slider.highValue, value) * 100);
                slider.tooltip = field.Label + " · " + slider.lowValue.ToString("N0") + "–" + slider.highValue.ToString("N0") + " HP";
            }

            void Commit(float value)
            {
                if (value.Equals(initial)) return;
                initial = value;
                edit(value.ToString("R", CultureInfo.InvariantCulture));
            }

            void FinishDrag()
            {
                if (!dragging) return;
                dragging = false; Commit(input.value);
            }

            slider.RegisterCallback<PointerDownEvent>(evt => { if (evt.button == 0) dragging = true; }, TrickleDown.TrickleDown);
            slider.Q("unity-drag-container").RegisterCallback<PointerUpEvent>(_ => FinishDrag());
            slider.RegisterCallback<PointerCaptureOutEvent>(_ => FinishDrag());
            slider.RegisterValueChangedCallback(evt =>
            {
                Display(Mathf.Round(evt.newValue));
                if (!dragging) Commit(input.value);
            });
            input.RegisterValueChangedCallback(evt => { Display(evt.newValue); Commit(evt.newValue); });
            Display(initial);
            return control;
        }
    }
}
