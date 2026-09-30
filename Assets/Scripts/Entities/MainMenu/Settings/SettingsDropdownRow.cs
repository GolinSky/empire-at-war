using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public class SettingsDropdownRow : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropdown;

        public event Action<int> ValueChanged;

        public void Initialize()
        {
            dropdown.onValueChanged.AddListener(RaiseValueChanged);
        }

        public void Dispose()
        {
            dropdown.onValueChanged.RemoveListener(RaiseValueChanged);
        }

        public void Render(SettingsChoice choice)
        {
            // Rebuilding identical options would reset the dropdown list while it is open.
            if (!HasSameOptions(choice.Options))
            {
                dropdown.options.Clear();
                foreach (string option in choice.Options)
                {
                    dropdown.options.Add(new TMP_Dropdown.OptionData(option));
                }
            }

            dropdown.SetValueWithoutNotify(choice.Index);
            dropdown.RefreshShownValue();
            dropdown.interactable = choice.Interactable;
        }

        private bool HasSameOptions(IReadOnlyList<string> options)
        {
            if (dropdown.options.Count != options.Count)
            {
                return false;
            }

            for (int i = 0; i < options.Count; i++)
            {
                if (dropdown.options[i].text != options[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void RaiseValueChanged(int index)
        {
            ValueChanged?.Invoke(index);
        }
    }
}
