using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ui;
using EmpireAtWar.Controllers.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public sealed class FactionProductionMenus : MonoBehaviour
    {
        [SerializeField] private Button researchButton;
        [SerializeField] private Button structuresButton;
        [SerializeField] private HudButtonAttention researchAttention;
        [SerializeField] private HudButtonAttention structuresAttention;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform popup;
        [SerializeField] private RectTransform researchCards;
        [SerializeField] private RectTransform structureCards;
        [SerializeField] private TMP_Text title;

        private readonly MenuAttention _research = new();
        private readonly MenuAttention _structures = new();

        public RectTransform ResearchCards => researchCards;
        public RectTransform StructureCards => structureCards;
        public bool IsOpen => popup.gameObject.activeSelf;
        public event Action<bool> OpenChanged;

        public void Initialize()
        {
            researchButton.onClick.AddListener(ToggleResearch);
            structuresButton.onClick.AddListener(ToggleStructures);
            closeButton.onClick.AddListener(Close);
            SetAvailable(false);
        }

        public void Dispose()
        {
            researchButton.onClick.RemoveListener(ToggleResearch);
            structuresButton.onClick.RemoveListener(ToggleStructures);
            closeButton.onClick.RemoveListener(Close);
            Close();
            _research.Reset(researchAttention);
            _structures.Reset(structuresAttention);
        }

        public void SetPopupParent(Transform parent) => popup.SetParent(parent, false);

        public void SetAvailable(bool available)
        {
            researchButton.gameObject.SetActive(available);
            structuresButton.gameObject.SetActive(available);
            if (!available) Close();
        }

        public void RefreshAvailableCards(IReadOnlyList<FactionUnitUi> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                FactionUnitUi card = cards[i];
                if (!card.gameObject.activeSelf) continue;
                if (card.transform.parent == researchCards) _research.Add(card.Request);
                else if (card.transform.parent == structureCards) _structures.Add(card.Request);
            }

            _research.Refresh(researchAttention, IsOpen && researchCards.gameObject.activeSelf);
            _structures.Refresh(structuresAttention, IsOpen && structureCards.gameObject.activeSelf);
        }

        public void DismissOutside(Vector2 position)
        {
            if (IsOpen && !RectTransformUtility.RectangleContainsScreenPoint(popup, position) &&
                !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)researchButton.transform, position) &&
                !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)structuresButton.transform, position))
                Close();
        }

        public void Close()
        {
            if (!IsOpen) return;
            popup.gameObject.SetActive(false);
            OpenChanged?.Invoke(false);
        }

        private void ToggleResearch() => Toggle(true);
        private void ToggleStructures() => Toggle(false);

        private void Toggle(bool research)
        {
            if (IsOpen && research == researchCards.gameObject.activeSelf)
            {
                Close();
                return;
            }

            title.text = research ? "Research" : "Structures";
            researchCards.gameObject.SetActive(research);
            structureCards.gameObject.SetActive(!research);
            if (research) _research.Acknowledge(researchAttention);
            else _structures.Acknowledge(structuresAttention);
            popup.SetAsLastSibling();
            if (IsOpen) return;
            popup.gameObject.SetActive(true);
            OpenChanged?.Invoke(true);
        }

        private void OnDestroy()
        {
            if (popup != null && !popup.IsChildOf(transform))
                Destroy(popup.gameObject);
        }

        private sealed class MenuAttention
        {
            private HashSet<UnitRequest> _visible = new();
            private HashSet<UnitRequest> _next = new();
            private readonly HashSet<UnitRequest> _unread = new();

            public void Add(UnitRequest request) => _next.Add(request);

            public void Refresh(HudButtonAttention attention, bool open)
            {
                if (!open)
                {
                    foreach (UnitRequest request in _next)
                    {
                        if (!_visible.Contains(request)) _unread.Add(request);
                    }
                }

                _unread.IntersectWith(_next);
                (_visible, _next) = (_next, _visible);
                _next.Clear();
                attention.SetReady(_unread.Count > 0, pulse: true);
            }

            public void Acknowledge(HudButtonAttention attention)
            {
                _unread.Clear();
                attention.SetReady(false, pulse: true);
            }

            public void Reset(HudButtonAttention attention)
            {
                _visible.Clear();
                Acknowledge(attention);
            }
        }
    }
}
