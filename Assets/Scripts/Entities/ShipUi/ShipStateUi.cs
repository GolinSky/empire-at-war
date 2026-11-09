using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.ShipUi
{
    public sealed class ShipStateUi : MonoBehaviour
    {
        private const int COLUMNS = 2;
        private const float ICON_SIZE = 18f;
        private const float ICON_SPACING = 2f;
        private const int GUARD = 1;
        private const int HUNT = 2;
        private const int ATTACK_MOVE = 4;

        [SerializeField] private Image iconTemplate;
        [SerializeField] private Sprite guardIcon;
        [SerializeField] private Sprite huntIcon;
        [SerializeField] private Sprite attackMoveIcon;

        private readonly List<Image> _icons = new List<Image>();
        private readonly HashSet<ShipAbilityId> _activeAbilities = new HashSet<ShipAbilityId>();
        private readonly List<IUnitOrderObserverFacade> _orders = new List<IUnitOrderObserverFacade>();
        private readonly List<ShipAbilitySlot> _slots = new List<ShipAbilitySlot>();
        private readonly List<bool> _activeStates = new List<bool>();
        private int _orderMask;

        public void Configure(IReadOnlyList<IUnitOrderObserverFacade> orders,
            IReadOnlyList<ShipAbilitySlot> slots)
        {
            _orders.Clear();
            _slots.Clear();
            _activeStates.Clear();
            for (int i = 0; i < orders.Count; i++) _orders.Add(orders[i]);
            for (int i = 0; i < slots.Count; i++)
            {
                _slots.Add(slots[i]);
                _activeStates.Add(slots[i].State == ShipAbilityState.Active);
            }
            Refresh();
        }

        public void Clear()
        {
            _orders.Clear();
            _slots.Clear();
            _activeStates.Clear();
            Refresh();
        }

        private void OnEnable() => Refresh();

        private void Update()
        {
            bool changed = _orderMask != GetOrderMask();
            for (int i = 0; i < _slots.Count; i++)
                changed |= _activeStates[i] != (_slots[i].State == ShipAbilityState.Active);
            if (changed) Refresh();
        }

        private int GetOrderMask()
        {
            int mask = 0;
            for (int i = 0; i < _orders.Count; i++)
            {
                switch (_orders[i].CurrentOrder)
                {
                    case UnitOrderType.Guard: mask |= GUARD; break;
                    case UnitOrderType.Hunt: mask |= HUNT; break;
                    case UnitOrderType.AttackMove: mask |= ATTACK_MOVE; break;
                }
            }
            return mask;
        }

        private void Refresh()
        {
            iconTemplate.gameObject.SetActive(false);
            _orderMask = GetOrderMask();
            int count = 0;
            if ((_orderMask & GUARD) != 0) ShowIcon(count++, guardIcon);
            if ((_orderMask & HUNT) != 0) ShowIcon(count++, huntIcon);
            if ((_orderMask & ATTACK_MOVE) != 0) ShowIcon(count++, attackMoveIcon);
            _activeAbilities.Clear();
            for (int i = 0; i < _slots.Count; i++)
            {
                ShipAbilitySlot slot = _slots[i];
                _activeStates[i] = slot.State == ShipAbilityState.Active;
                if (_activeStates[i] && _activeAbilities.Add(slot.Id))
                    ShowIcon(count++, slot.Definition.Icon);
            }
            for (int i = count; i < _icons.Count; i++)
                _icons[i].gameObject.SetActive(false);
        }

        private void ShowIcon(int index, Sprite sprite)
        {
            if (index == _icons.Count)
            {
                Image icon = Instantiate(iconTemplate, iconTemplate.transform.parent, false);
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                RectTransform rect = icon.rectTransform;
                rect.anchorMin = Vector2.one;
                rect.anchorMax = Vector2.one;
                rect.pivot = Vector2.one;
                rect.sizeDelta = new Vector2(ICON_SIZE, ICON_SIZE);
                rect.anchoredPosition = new Vector2(-(index % COLUMNS) * (ICON_SIZE + ICON_SPACING),
                    -(index / COLUMNS) * (ICON_SIZE + ICON_SPACING));
                _icons.Add(icon);
            }
            _icons[index].sprite = sprite;
            _icons[index].gameObject.SetActive(true);
        }
    }
}
