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
        [SerializeField] private Button removeButton;
        private Action<int> _selected;
        private Action<int> _removed;
        private int _index;

        public void Initialize(int index, string text, bool enabled, Action<int> selected, Action<int> removed)
        {
            _index = index;
            _selected = selected;
            _removed = removed;
            label.text = text;
            button.interactable = enabled;
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
            removeButton.onClick.RemoveListener(Remove);
            removeButton.onClick.AddListener(Remove);
        }

        private void Select() => _selected(_index);
        private void Remove() => _removed(_index);
        private void OnDestroy()
        {
            button.onClick.RemoveListener(Select);
            removeButton.onClick.RemoveListener(Remove);
        }
    }
}
