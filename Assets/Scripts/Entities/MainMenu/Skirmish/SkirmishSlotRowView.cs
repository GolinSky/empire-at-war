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
        [SerializeField] private Button colorButton;
        [SerializeField] private TMP_Text colorLabel;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private Graphic colorEdge;
        [SerializeField] private CanvasGroup configurationGroup;
        [SerializeField] private CanvasGroup slotLabelGroup;
        [SerializeField] private TMP_Text slotIndexText;
        [SerializeField] private RawImage avatarImage;

        private int _rowIndex;
        private int _memberId;
        private int _occupantIndex;
        private SkirmishSlotOccupant _occupant;
        private bool _occupantEditable;
        private bool _discardOccupantSelection;
        private readonly List<string> _occupantOptions = new List<string>();

        public event Action<int, int> OccupantChanged;

        public event Action<int, int> FactionChanged;

        public event Action<int, int> TeamChanged;

        public event Action<int> ColorRequested;

        public Button ColorButton => colorButton;

        public void Initialize(
            int rowIndex,
            IReadOnlyList<string> factionOptions,
            IReadOnlyList<string> teamOptions)
        {
            _rowIndex = rowIndex;
            slotIndexText.text = (rowIndex + 1).ToString("00");
            SetOptions(factionDropdown, factionOptions);
            SetOptions(teamDropdown, teamOptions);

            occupantDropdown.onValueChanged.AddListener(HandleOccupantChanged);
            factionDropdown.onValueChanged.AddListener(HandleFactionChanged);
            teamDropdown.onValueChanged.AddListener(HandleTeamChanged);
            colorButton.onClick.AddListener(RequestColor);
        }

        public void Dispose()
        {
            occupantDropdown.onValueChanged.RemoveListener(HandleOccupantChanged);
            factionDropdown.onValueChanged.RemoveListener(HandleFactionChanged);
            teamDropdown.onValueChanged.RemoveListener(HandleTeamChanged);
            colorButton.onClick.RemoveListener(RequestColor);
        }

        public void Render(SkirmishSlotSetup slot, Color playerColor, string colorName, SkirmishSlotRowState state)
        {
            bool optionsChanged = !_occupantOptions.SequenceEqual(state.OccupantOptions);
            if (occupantDropdown.IsExpanded && (optionsChanged || _memberId != slot.MemberId ||
                _occupant != slot.Occupant || _occupantEditable != state.OccupantEditable))
            {
                // TMP keeps the old popup alive during its closing fade.
                _discardOccupantSelection = true;
                occupantDropdown.Hide();
            }

            _memberId = slot.MemberId;
            _occupant = slot.Occupant;
            _occupantIndex = state.OccupantIndex;
            _occupantEditable = state.OccupantEditable;
            if (optionsChanged)
            {
                _occupantOptions.Clear();
                _occupantOptions.AddRange(state.OccupantOptions);
                SetOptions(occupantDropdown, _occupantOptions);
            }

            bool hasAvatar = state.Avatar != null;
            avatarImage.gameObject.SetActive(hasAvatar);
            avatarImage.texture = state.Avatar;
            slotIndexText.gameObject.SetActive(!hasAvatar);
            titleText.text = state.Title;
            roleText.text = state.Role;
            occupantDropdown.SetValueWithoutNotify(state.OccupantIndex);
            factionDropdown.SetValueWithoutNotify((int)slot.Faction);
            teamDropdown.SetValueWithoutNotify(slot.Team);
            string hex = ColorUtility.ToHtmlStringRGB(playerColor);
            colorLabel.text = $"<mark=#{hex}FF padding=\"0,0,-20,-20\">  </mark>  {colorName}";
            occupantDropdown.interactable = state.OccupantEditable && !_discardOccupantSelection;
            factionDropdown.interactable = state.DetailsEditable;
            teamDropdown.interactable = state.TeamEditable;
            colorButton.interactable = state.DetailsEditable;
            occupantDropdown.RefreshShownValue();
            factionDropdown.RefreshShownValue();
            teamDropdown.RefreshShownValue();
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

        private void LateUpdate()
        {
            if (_discardOccupantSelection && !occupantDropdown.IsExpanded)
            {
                _discardOccupantSelection = false;
                occupantDropdown.interactable = _occupantEditable;
            }
        }

        private void HandleOccupantChanged(int value)
        {
            if (_discardOccupantSelection || !occupantDropdown.interactable)
            {
                occupantDropdown.SetValueWithoutNotify(_occupantIndex);
                return;
            }

            OccupantChanged?.Invoke(_rowIndex, value);
        }

        private void HandleFactionChanged(int value) => FactionChanged?.Invoke(_rowIndex, value);

        private void HandleTeamChanged(int value) => TeamChanged?.Invoke(_rowIndex, value);

        private void RequestColor() => ColorRequested?.Invoke(_rowIndex);
    }
}
