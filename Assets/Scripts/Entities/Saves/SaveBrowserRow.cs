using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Saves
{
    public sealed class SaveBrowserRow : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        private Action<int> _selected;
        private int _index;

        public void Initialize(int index, string text, bool enabled, Action<int> selected)
        {
            _index = index;
            _selected = selected;
            label.text = text;
            button.interactable = enabled;
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
        }

        private void Select() => _selected(_index);
        private void OnDestroy() => button.onClick.RemoveListener(Select);
    }
}
