using System;
using EmpireAtWar.Models.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views
{
    public sealed class ShipSelectionGroupUi : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text typeLabel;
        [SerializeField] private Transform entriesParent;
        [SerializeField] private ShipSelectionEntryUi entryPrefab;

        private ShipType _shipType;
        private Action<ShipType> _onClicked;

        private void Awake()
        {
            if (button == null || typeLabel == null || entriesParent == null || entryPrefab == null)
            {
                throw new InvalidOperationException($"{nameof(ShipSelectionGroupUi)} has an unassigned UI reference.");
            }

            button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(HandleClick);
        }

        public void Configure(ShipType shipType, Sprite icon, int amount,
            int visibleEntries, Action<ShipType> onClicked)
        {
            _shipType = shipType;
            _onClicked = onClicked;
            typeLabel.text = shipType.ToString();

            for (int i = 0; i < visibleEntries; i++)
            {
                ShipSelectionEntryUi entry = Instantiate(entryPrefab, entriesParent);
                entry.Configure(shipType, icon, amount, onClicked);
                entry.gameObject.SetActive(true);
            }
        }

        private void HandleClick()
        {
            _onClicked(_shipType);
        }
    }
}
