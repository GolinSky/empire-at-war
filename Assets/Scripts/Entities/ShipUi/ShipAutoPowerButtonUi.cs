using System;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Models.ShipUi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.ShipUi
{
    public sealed class ShipAutoPowerButtonUi : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject activeHighlight;
        [SerializeField] private TooltipTrigger tooltipTrigger;
        private Action _onPressed;

        public TooltipTrigger TooltipTrigger => tooltipTrigger;

        private void Awake() => button.onClick.AddListener(HandleClick);

        private void OnDestroy() => button.onClick.RemoveListener(HandleClick);

        public void Configure(ShipAutoPowerUiState state, Action onPressed)
        {
            _onPressed = onPressed;
            tooltipTrigger.SetKey(state);
            button.interactable = state.GlobalEnabled;
            activeHighlight.SetActive(state.GlobalEnabled && (state.Enabled || state.Mixed));
            label.text = "A<size=8>\n" + (state.Mixed ? "MIX" : state.Enabled ? "ON" : "OFF") + "</size>";
            label.color = state.GlobalEnabled
                ? new Color(0.97f, 0.98f, 0.99f, 1f)
                : new Color(0.45f, 0.5f, 0.6f, 1f);
            gameObject.SetActive(state.Visible);
        }

        private void HandleClick() => _onPressed();
    }
}
