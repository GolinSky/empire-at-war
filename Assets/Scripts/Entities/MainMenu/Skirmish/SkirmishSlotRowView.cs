using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    /// <summary>
    /// One player row of the skirmish setup: who plays it, which faction and which team.
    /// Forwards every change with its row index so the skirmish UI can pass it straight to the route navigation.
    /// </summary>
    public class SkirmishSlotRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Dropdown occupantDropdown;
        [SerializeField] private TMP_Dropdown factionDropdown;
        [SerializeField] private TMP_Dropdown teamDropdown;
        [SerializeField] private TMP_Dropdown colorDropdown;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private Graphic colorEdge;
        [SerializeField] private CanvasGroup configurationGroup;
        [SerializeField] private CanvasGroup slotLabelGroup;

        private int _rowIndex;
        private readonly List<string> _occupantOptions = new List<string>();

        public event Action<int, int> OccupantChanged;

        public event Action<int, int> FactionChanged;

        public event Action<int, int> TeamChanged;

        public event Action<int, int> ColorChanged;

        public void Initialize(
            int rowIndex,
            IReadOnlyList<string> factionOptions,
            IReadOnlyList<string> teamOptions,
            IReadOnlyList<string> colorOptions)
        {
            _rowIndex = rowIndex;
            SetOptions(factionDropdown, factionOptions);
            SetOptions(teamDropdown, teamOptions);
            SetOptions(colorDropdown, colorOptions);

            occupantDropdown.onValueChanged.AddListener(HandleOccupantChanged);
            factionDropdown.onValueChanged.AddListener(HandleFactionChanged);
            teamDropdown.onValueChanged.AddListener(HandleTeamChanged);
            colorDropdown.onValueChanged.AddListener(HandleColorChanged);
        }

        public void Dispose()
        {
            occupantDropdown.onValueChanged.RemoveListener(HandleOccupantChanged);
            factionDropdown.onValueChanged.RemoveListener(HandleFactionChanged);
            teamDropdown.onValueChanged.RemoveListener(HandleTeamChanged);
            colorDropdown.onValueChanged.RemoveListener(HandleColorChanged);
        }

        public void Render(SkirmishSlotSetup slot, Color playerColor, SkirmishSlotRowState state)
        {
            if (!_occupantOptions.SequenceEqual(state.OccupantOptions))
            {
                _occupantOptions.Clear();
                _occupantOptions.AddRange(state.OccupantOptions);
                SetOptions(occupantDropdown, _occupantOptions);
            }

            titleText.text = state.Title;
            roleText.text = state.Role;
            occupantDropdown.SetValueWithoutNotify(state.OccupantIndex);
            factionDropdown.SetValueWithoutNotify((int)slot.Faction);
            teamDropdown.SetValueWithoutNotify(slot.Team);
            colorDropdown.SetValueWithoutNotify(slot.ColorIndex);
            occupantDropdown.interactable = state.OccupantEditable;
            factionDropdown.interactable = state.DetailsEditable;
            teamDropdown.interactable = state.DetailsEditable;
            colorDropdown.interactable = state.DetailsEditable;
            occupantDropdown.RefreshShownValue();
            factionDropdown.RefreshShownValue();
            teamDropdown.RefreshShownValue();
            colorDropdown.RefreshShownValue();
            colorEdge.color = playerColor;
            colorEdge.enabled = slot.IsOpen;
            configurationGroup.alpha = slot.IsOpen ? 1f : 0.38f;
            slotLabelGroup.alpha = slot.IsOpen ? 1f : 0.5f;
        }

        private static void SetOptions(TMP_Dropdown dropdown, IReadOnlyList<string> options)
        {
            dropdown.options.Clear();
            foreach (string option in options)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(option));
            }

            dropdown.SetValueWithoutNotify(0);
            dropdown.RefreshShownValue();
        }

        private void HandleOccupantChanged(int value) => OccupantChanged?.Invoke(_rowIndex, value);

        private void HandleFactionChanged(int value) => FactionChanged?.Invoke(_rowIndex, value);

        private void HandleTeamChanged(int value) => TeamChanged?.Invoke(_rowIndex, value);

        private void HandleColorChanged(int value) => ColorChanged?.Invoke(_rowIndex, value);
    }
}
