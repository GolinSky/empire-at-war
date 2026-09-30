using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public class KeyBindingRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private Button bindingButton;
        [SerializeField] private TMP_Text bindingText;
        [SerializeField] private Button resetButton;

        private int _row;
        private Action<int> _rebind;
        private Action<int> _reset;

        public void Initialize(int row, Action<int> rebind, Action<int> reset)
        {
            _row = row;
            _rebind = rebind;
            _reset = reset;
            bindingButton.onClick.AddListener(RequestRebind);
            resetButton.onClick.AddListener(RequestReset);
        }

        public void Dispose()
        {
            bindingButton.onClick.RemoveListener(RequestRebind);
            resetButton.onClick.RemoveListener(RequestReset);
        }

        public void Render(KeyBindingRowState state)
        {
            actionText.text = state.ActionLabel;
            bindingText.text = state.BindingLabel;
        }

        private void RequestRebind()
        {
            _rebind(_row);
        }

        private void RequestReset()
        {
            _reset(_row);
        }
    }
}
