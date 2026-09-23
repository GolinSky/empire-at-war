using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;

namespace EmpireAtWar.Views
{
    public sealed class ShipAbilityBarUi : MonoBehaviour
    {
        [SerializeField] private ShipAbilityButtonUi buttonPrefab;
        [SerializeField] private Transform buttonParent;

        private readonly List<ShipAbilityButtonUi> _buttons = new List<ShipAbilityButtonUi>();
        private readonly Dictionary<ShipAbilityId, List<ShipAbilitySlot>> _groups =
            new Dictionary<ShipAbilityId, List<ShipAbilitySlot>>();
        private IShipUiModelObserver _model;

        public void SetModel(IShipUiModelObserver model) => _model = model;

        public void SetSlots(IReadOnlyList<ShipAbilitySlot> slots, Action<ShipAbilityId> onPressed)
        {
            foreach (List<ShipAbilitySlot> group in _groups.Values) group.Clear();
            for (int i = 0; i < slots.Count; i++)
            {
                ShipAbilitySlot slot = slots[i];
                if (!_groups.TryGetValue(slot.Id, out List<ShipAbilitySlot> group))
                {
                    group = new List<ShipAbilitySlot>();
                    _groups.Add(slot.Id, group);
                }
                group.Add(slot);
            }

            int buttonIndex = 0;
            foreach (KeyValuePair<ShipAbilityId, List<ShipAbilitySlot>> group in _groups)
            {
                if (group.Value.Count == 0) continue;
                if (buttonIndex == _buttons.Count)
                    _buttons.Add(Instantiate(buttonPrefab, buttonParent));
                _buttons[buttonIndex].Configure(group.Key, group.Value,
                    group.Value[0].Definition.Icon, onPressed);
                _buttons[buttonIndex].gameObject.SetActive(true);
                buttonIndex++;
            }
            for (int i = buttonIndex; i < _buttons.Count; i++)
                _buttons[i].gameObject.SetActive(false);
        }

        private void Update()
        {
            for (int i = 0; i < _buttons.Count; i++)
                if (_buttons[i].gameObject.activeSelf)
                    _buttons[i].SetWaiting(_model.PendingAbilityId == _buttons[i].Id);
        }
    }
}
